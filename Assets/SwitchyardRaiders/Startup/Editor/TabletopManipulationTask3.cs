using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Oculus.Interaction;
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class TabletopManipulationTask3
    {
        static void Field(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
        static void Call(object o,string n,object v)=>o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,new[]{v});
        sealed class Fingers:IFingerAPI
        {
            public HandFingerFlags flags;
            public bool GetFingerIsGrabbing(HandFinger f)=>(flags&(HandFingerFlags)(1<<(int)f))!=0;
            public bool GetFingerIsGrabbingChanged(HandFinger f,bool target)=>false;
            public float GetFingerGrabScore(HandFinger f)=>GetFingerIsGrabbing(f)?1:0;
            public Vector3 GetWristOffsetLocal()=>Vector3.zero;
            public void Update(IHand h){}
        }
        sealed class TrackedHand:IHand
        {
            public bool confidence=true,valid=true;
            public Handedness Handedness=>Handedness.Left;
            public bool IsConnected=>valid;
            public bool IsHighConfidence=>confidence;
            public bool IsDominantHand=>false;
            public float Scale=>1;
            public bool IsPointerPoseValid=>valid;
            public bool IsTrackedDataValid=>valid;
            public int CurrentDataVersion=>1;
            public event Action WhenHandUpdated {add{}remove{}}
            public bool GetFingerIsPinching(HandFinger f)=>true; // deliberately pinching: ignored by adapter
            public bool GetIndexFingerIsPinching()=>true;
            public bool GetFingerIsHighConfidence(HandFinger f)=>confidence;
            public float GetFingerPinchStrength(HandFinger f)=>1;
            public bool GetRootPose(out Pose p){p=new Pose(new Vector3(2,1,3),Quaternion.identity);return valid;}
            public bool GetPointerPose(out Pose p)=>GetRootPose(out p);
            public bool GetJointPose(HandJointId id,out Pose p)=>GetRootPose(out p);
            public bool GetJointPoseLocal(HandJointId id,out Pose p)=>GetRootPose(out p);
            public bool GetJointPoseFromWrist(HandJointId id,out Pose p)=>GetRootPose(out p);
            public bool GetPalmPoseLocal(out Pose p)=>GetRootPose(out p);
            public bool GetJointPosesLocal(out ReadOnlyHandJointPoses p){p=default;return valid;}
            public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses p){p=default;return valid;}
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/8 - Validate two-hand view and room motion")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))throw new Exception("Correct project/Edit Mode required.");
            int count=0;var log=new StringBuilder("Task 3: synthetic Edit Mode inputs, not live hand recognition.\n");
            void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;log.AppendLine("PASS "+message);}
            bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.00002f;
            var fixture=new GameObject("Task3 inactive input fixture");fixture.SetActive(false);
            try
            {
                Transform Node(string n){var g=new GameObject(n);g.transform.SetParent(fixture.transform,false);return g.transform;}
                var t=fixture.AddComponent<RaidersTable>();var room=fixture.AddComponent<RoomChoiceMenu>();var s=fixture.AddComponent<RaidersSession>();var launch=fixture.AddComponent<LaunchSequence>();var input=fixture.AddComponent<RaidersTableManipulation>();
                t.roomMenu=room;t.session=s;t.launch=launch;t.manipulation=input;input.table=t;s.table=t;
                t.presentation=Node("Miniature");t.environmentMotion=Node("Room");t.environmentMotion.position=new Vector3(3,0,4);
                t.root=Node("Gameplay");t.virtualTable=Node("Tray").gameObject;t.terrainAssembly=Node("Land").gameObject;t.humanPresentation=Node("Human seats");
                launch.headset=Node("Tracked head");launch.headset.position=new Vector3(1,1.4f,-2);launch.headset.rotation=Quaternion.Euler(0,73,0);
                var originalHead=new Pose(launch.headset.position,launch.headset.rotation);
                Field(room,"<IsVirtual>k__BackingField",true);Field(room,"<HasSelection>k__BackingField",true);Field(s,"<Phase>k__BackingField",RaidersSessionPhase.Playing);
                t.ModeChosen();var home=t.presentation.position;var roomHome=t.environmentMotion.position;
                float now=Time.unscaledTime+10;Vector3 l=new Vector3(3,1,4),r=new Vector3(3.4f,1.3f,4); // far outside tray
                void Step(bool lc=true,bool rc=true,bool valid=true){now+=.11f;input.ProcessHands(new RaidersTableManipulation.HandSample(valid,lc,l),new RaidersTableManipulation.HandSample(valid,rc,r),now);}
                void Acquire(){Step(false,false);Step();Step();}
                Acquire();Check(input.Dragging && Near(t.presentation.position,home),"Off-table acquisition is stable and does not jump, including a 73 degree rig yaw");
                Step(false,false);l+=Vector3.right;Step(true,false);r+=Vector3.right;Step(true,false);
                Check(!input.Dragging && Near(t.presentation.position,home),"One closed hand cannot move the view");
                Step();Check(input.State==RaidersTableManipulation.GrabState.Acquiring,"Second hand enters stable acquisition");
                Step();Check(input.Dragging && Near(t.presentation.position,home),"Second-hand acquisition captures fresh baseline");
                var old=t.presentation.position;var oldRoom=t.environmentMotion.position;var delta=new Vector3(.2f,.12f,-.1f);l+=delta;r+=delta;Step();
                Check(Near(t.presentation.position,old+delta)&&Near(t.environmentMotion.position,oldRoom+delta),"Common deliberate XYZ translation moves table and VR room coherently");
                var mid=(l+r)*.5f;old=t.presentation.position;float sy=t.RelativeZoom;var q=Quaternion.Euler(0,40,0);l=mid+q*(l-mid);r=mid+q*(r-mid);Step();
                Check(Mathf.Abs(t.presentation.position.y-old.y)<.00002f&&Mathf.Abs(t.RelativeZoom-sy)<.00002f,"Yaw with asymmetric hand heights produces no vertical drift or zoom");
                var pivotLocal=(Vector3)typeof(RaidersTableManipulation).GetField("localPivot",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(input);
                var pivot=t.presentation.TransformPoint(pivotLocal);oldRoom=t.environmentMotion.position;var roomRot=t.environmentMotion.rotation;
                mid=(l+r)*.5f;var dl=Vector3.ProjectOnPlane(l-mid,Vector3.up);var dr=Vector3.ProjectOnPlane(r-mid,Vector3.up);l+=dl;r+=dr;Step();
                Check(Mathf.Abs(t.RelativeZoom-2)<.00002f && Near(t.presentation.TransformPoint(pivotLocal),pivot),"Scale doubles planar span around fixed terrain point, ignoring unequal hand heights");
                Check(Near(t.environmentMotion.position,oldRoom)&&Quaternion.Angle(t.environmentMotion.rotation,roomRot)<.001f,"Pure scale does not move or rotate the room");
                old=t.presentation.position;sy=t.RelativeZoom;l+=Vector3.up*.4f;Step();
                Check(Near(t.presentation.position,old)&&Mathf.Abs(t.RelativeZoom-sy)<.00002f,"Lifting only one hand cannot cause vertical drift or zoom");
                mid=(l+r)*.5f;l=mid+Vector3.left*10;r=mid+Vector3.right*10;Step();Check(t.RelativeZoom==6,"Upper zoom bound is exactly 6");
                old=t.presentation.position;Step();Check(Near(t.presentation.position,old),"Stationary upper overshoot cannot drift");
                l+=Vector3.right*.01f;r-=Vector3.right*.01f;Step();Check(t.RelativeZoom<6,"Maximum clamp reverses immediately");
                l=mid+Vector3.left*.05f;r=mid+Vector3.right*.05f;Step();Check(t.RelativeZoom==1,"Lower zoom bound is exactly 1");
                l-=Vector3.right*.005f;r+=Vector3.right*.005f;Step();Check(t.RelativeZoom>1,"Minimum clamp reverses immediately");
                old=t.presentation.position;Step(false,true);l+=Vector3.one;r+=Vector3.one;Step(false,true);
                Check(Near(t.presentation.position,old)&&!input.Dragging,"Release either hand freezes all movement");
                Step();Step();Check(Near(t.presentation.position,old)&&input.Dragging,"Regrab captures fresh hands without jumping");
                Step(true,true,false);Check(input.State==RaidersTableManipulation.GrabState.AwaitOpen,"Tracking/confidence loss suspends");
                l+=Vector3.one;r+=Vector3.one;Step();Step();Check(!input.Dragging&&Near(t.presentation.position,old),"Tracking recovery while clenched cannot jump or resume");
                Acquire();Call(input,"OnApplicationFocus",false);Step();Check(!input.Dragging,"Focus loss freezes input");Call(input,"OnApplicationFocus",true);Acquire();
                Call(input,"OnApplicationPause",true);Step();Check(!input.Dragging,"Application pause freezes input");Call(input,"OnApplicationPause",false);Acquire();
                t.PrepareModeChange();Step();Check(!input.Dragging,"Mode transition requires both hands open before reacquisition");
                t.ModeChosen();Acquire();Check(input.Dragging,"VR restoration supports fresh acquisition");
                Check(t.environmentMotion.lossyScale==Vector3.one,"Room scale remains exactly one through combined gestures and clamps");
                t.ResetView();Check(t.RelativeZoom==1&&Near(t.environmentMotion.position,roomHome),"Reset restores normal table and original room pose");
                Check(Near(launch.headset.position,originalHead.position)&&Quaternion.Angle(launch.headset.rotation,originalHead.rotation)<.001f&&launch.headset.lossyScale==Vector3.one,"Physical tracked head never moved, rotated or scaled");
                Field(room,"<IsVirtual>k__BackingField",false);Field(t,"homeScale",.6f);Field(t,"<Exploration>k__BackingField",true);t.presentation.localScale=Vector3.one*.6f;
                oldRoom=t.environmentMotion.position;roomRot=t.environmentMotion.rotation;Acquire();l+=delta;r+=delta;Step();
                Check(Near(t.environmentMotion.position,oldRoom)&&Quaternion.Angle(t.environmentMotion.rotation,roomRot)<.001f,"MR moves virtual content without moving the room");
                Check(t.RelativeZoom>=1&&t.RelativeZoom<=6,"MR retains relative zoom contract");
                t.roomUnderstanding=fixture.AddComponent<RaidersRoom>();Field(t,"<Exploration>k__BackingField",false);old=t.presentation.position;l+=delta;r+=delta;Step();
                Check(Near(t.presentation.position,old),"Unconfirmed/out-of-bounds anchored MR motion is rejected");
                Field(room,"<IsVirtual>k__BackingField",true);t.ResetView();Acquire();int epoch=input.InteractionEpoch;Check(input.BlocksGameplay,"Active gesture suppresses commands");
                Step(false,false);Check(input.InteractionEpoch!=epoch&&input.BlocksGameplay,"Release cancels old UI presses and guards commands");
                Field(input,"releaseUntil",-1f);Check(!input.BlocksGameplay,"Ordinary menus recover after release guard");
                var match=new RaidersMatch();match.Reset(false,4);Field(s,"<Match>k__BackingField",match);Field(s,"<IsLocal>k__BackingField",true);
                var menu=fixture.AddComponent<RaidersMenu>();menu.table=t;menu.session=s;
                Acquire();float throttle=match.players[0].throttle;menu.Invoke("drive");s.Command(RaidersAction.Throttle,0,1);
                Check(match.players[0].throttle==throttle,"Menu and session command routes both reject active-gesture gameplay commands");
                var action=fixture.AddComponent<RaidersMenuAction>();action.menu=menu;action.command="aimleft";Field(action,"pressedEpoch",input.InteractionEpoch);
                Step(false,false);Field(input,"releaseUntil",-1f);action.Invoke();
                Check((float)typeof(RaidersMenu).GetField("aim",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(menu)==0,"UI click begun before release is canceled even after guard expires");
                menu.Invoke("aimleft");Check((float)typeof(RaidersMenu).GetField("aim",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(menu)<0,"Ordinary menu commands work again after release guard");
                var api=fixture.AddComponent<HandGrabAPI>();var fingers=new Fingers();api.InjectOptionalFingerGrabAPI(fingers);
                fingers.flags=HandFingerFlags.Thumb|HandFingerFlags.Index;Check(!RaidersTableManipulation.PalmClosed(api.HandPalmGrabbingFingers()),"Official API thumb/index-only flags cannot substitute a palm grab");
                fingers.flags=HandFingerFlags.Middle|HandFingerFlags.Ring|HandFingerFlags.Pinky;Check(RaidersTableManipulation.PalmClosed(api.HandPalmGrabbingFingers()),"Official palm API three-finger closure recognized");
                foreach(var finger in new[]{HandFingerFlags.Middle,HandFingerFlags.Ring,HandFingerFlags.Pinky}){fingers.flags=(HandFingerFlags.Middle|HandFingerFlags.Ring|HandFingerFlags.Pinky)&~finger;Check(!RaidersTableManipulation.PalmClosed(api.HandPalmGrabbingFingers()),"Opening "+finger+" releases palm");}
                Check(!RaidersTableManipulation.Read(null,null).valid,"Missing hand data is invalid, never a fabricated pose");
                var tracked=fixture.AddComponent<OVRHand>();var hand=new TrackedHand();api.InjectHand(hand);
                Field(tracked,"<IsTracked>k__BackingField",true);Field(tracked,"<HandConfidence>k__BackingField",OVRHand.TrackingConfidence.High);
                fingers.flags=HandFingerFlags.All;
                Check(RaidersTableManipulation.Read(api,tracked).valid&&RaidersTableManipulation.Read(api,tracked).closed,"Adapter consumes injected official IHand and palm API data");
                Field(tracked,"<IsSystemGestureInProgress>k__BackingField",true);Check(!RaidersTableManipulation.Read(api,tracked).valid,"System gesture invalidates adapter input immediately");
                Field(tracked,"<IsSystemGestureInProgress>k__BackingField",false);hand.confidence=false;Check(!RaidersTableManipulation.Read(api,tracked).valid,"ISDK low confidence invalidates input");
                hand.confidence=true;hand.valid=false;Check(!RaidersTableManipulation.Read(api,tracked).valid,"Disconnected ISDK hand invalidates input");
                hand.valid=true;Field(tracked,"<HandConfidence>k__BackingField",OVRHand.TrackingConfidence.Low);Check(!RaidersTableManipulation.Read(api,tracked).valid,"OVR low confidence invalidates input");
                Field(tracked,"<HandConfidence>k__BackingField",OVRHand.TrackingConfidence.High);Field(tracked,"<IsTracked>k__BackingField",false);Check(!RaidersTableManipulation.Read(api,tracked).valid,"OVR tracking loss invalidates input");
                Check(!t.AcceptView(new Vector3(float.NaN,0,0),Quaternion.identity,1),"Nonfinite input rejected");
                // Independent analytic invariant over repeated combined steps and zoom extremes.
                Vector3 bp=new Vector3(0,.7f,1),pv=new Vector3(.15f,.85f,.9f);Quaternion br=Quaternion.identity;float bs=1;float maxDrift=0;
                for(int k=0;k<720;k++){
                    var a=new Vector3(-.2f,1,0);var b=new Vector3(.2f,1.35f,0);var yaw=Quaternion.Euler(0,.5f,0);float factor=k<360?1.02f:.98f;
                    var na=yaw*new Vector3(a.x*factor,a.y,a.z*factor);var nb=yaw*new Vector3(b.x*factor,b.y,b.z*factor);
                    TabletopViewMath.SolveAtPivot(bp,br,bs,pv,a,b,na,nb,1,6,out bp,out br,out bs,out var tr,out var turn);
                    maxDrift=Mathf.Max(maxDrift,Mathf.Abs((bp.y+TabletopViewContract.TerrainDatum*bs)-pv.y));
                }
                Check(maxDrift<.0001f,"720 yaw/zoom samples preserve terrain height within 0.1mm");
                log.AppendLine("Maximum measured terrain-height drift: "+maxDrift.ToString("G9")+" m");
                var start=new Vector3(.1f,.7f,1);var focus=new Vector3(.2f,.85f,1.1f);var left=new Vector3(-.2f,1,0);var right=new Vector3(.2f,1.3f,0);
                var turn30=Quaternion.Euler(0,30,0);var shift=new Vector3(.12f,.08f,-.09f);
                var newLeft=turn30*new Vector3(left.x*1.5f,left.y,left.z)+shift;var newRight=turn30*new Vector3(right.x*1.5f,right.y,right.z)+shift;
                TabletopViewMath.SolveAtPivot(start,Quaternion.identity,1,focus,left,right,newLeft,newRight,1,6,out var combined,out var combinedRot,out float combinedScale,out var common,out var commonYaw);
                Check(Near(combined,focus+shift+turn30*(start-focus)*1.5f)&&Mathf.Abs(combinedScale-1.5f)<.00001f&&Quaternion.Angle(combinedRot,turn30)<.001f,"Simultaneous XYZ translation, yaw and zoom match independent analytic solution");
                var roomPose=new Pose(new Vector3(4,0,2),Quaternion.identity);var rigid=TabletopViewMath.RigidMotion(roomPose,focus,common,commonYaw);
                Check(Near(rigid.position,focus+shift+turn30*(roomPose.position-focus))&&Quaternion.Angle(rigid.rotation,turn30)<.001f,"Room receives exactly common translation/yaw with scale omitted");
                TabletopViewMath.SolveAtPivot(start,Quaternion.identity,2,focus,left,right,Vector3.zero,Vector3.one*.001f,1,6,out _,out var degenerate,out float kept,out _,out _);
                Check(kept==2&&Quaternion.Angle(degenerate,Quaternion.identity)<.001f,"Near-coincident hands cannot flip yaw or divide scale by tiny span");
            }
            finally{Object.DestroyImmediate(fixture);}
            var scene=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            Check(scene&&scene.environmentMotion==scene.roomMenu.virtualRoom.transform,"Scene binds existing room as environment motion root");
            Check(scene.manipulation.leftPalm&&scene.manipulation.rightPalm&&scene.manipulation.leftPalm!=scene.manipulation.rightPalm,"Scene binds distinct existing palm classifiers");
            Check(new SerializedObject(scene.manipulation.leftPalm).FindProperty("_hand").objectReferenceValue.name=="LeftHand"&&new SerializedObject(scene.manipulation.rightPalm).FindProperty("_hand").objectReferenceValue.name=="RightHand","Scene classifier handedness comes from correct authored tracked nodes");
            Check(!scene.launch.transform.Find("Table rim hand grab"),"Obsolete collider/grabbable proxy removed");
            Check(!scene.launch.xrRigRoot.transform.IsChildOf(scene.environmentMotion)&&!scene.presentation.IsChildOf(scene.environmentMotion),"Tracking and scalable content are separate from rigid room");
            Check(scene.environmentMotion.lossyScale==Vector3.one&&!scene.environmentMotion.IsChildOf(scene.presentation),"Room is outside all zoom ancestry");
            Check(!typeof(ITransformer).IsAssignableFrom(typeof(RaidersTableManipulation)),"No competing ISDK transformer writes view");
            var prefab=PrefabUtility.LoadPrefabContents("Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab");
            try{var pt=prefab.GetComponentInChildren<RaidersTable>(true);Check(pt.environmentMotion&&pt.manipulation.leftPalm&&pt.manipulation.rightPalm&&pt.manipulation.leftPalm!=pt.manipulation.rightPalm&&!prefab.transform.Find("Table rim hand grab"),"Reusable launch prefab matches new scene bindings");}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            log.AppendLine(count+" assertions passed. No Play Mode, device session, or player build.");
            File.WriteAllText("Deliverables/DemeoRework/validation-task3.txt",log.ToString());Debug.Log(log.ToString());
        }
    }
}
