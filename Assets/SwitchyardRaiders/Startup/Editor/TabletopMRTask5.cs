using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Meta.XR.MRUtilityKit;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static partial class TabletopMRTask5
    {
        const string Launch="Assets/SwitchyardRaiders/Startup/Prefabs/SR_Launch.prefab";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Field(object o,string n,object v)=>o.GetType().GetField(n,Private).SetValue(o,v);
        static void Property(object o,string n,object v)=>o.GetType().GetProperty(n).SetValue(o,v);
        static void Guard(){if(EditorApplication.isPlayingOrWillChangePlaymode||!Application.dataPath.EndsWith("Switchyard Raiders/Assets")||EditorUtility.scriptCompilationFailed)throw new Exception("Compiled Switchyard Raiders Edit Mode required.");}
        [MenuItem("Switchyard Raiders/Demeo Rework/11 - Save MR baseline calibration")]
        public static void Apply()
        {
            Guard();var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            if(!table || table.gameObject.scene.isDirty)throw new Exception("Save/resolve existing scene work before calibration migration.");
            void Configure(RaidersRoom room)
            {if(!Mathf.Approximately(room.previousMRReferenceScale,1))throw new Exception("Existing custom MR calibration must be preserved/reviewed.");EditorUtility.SetDirty(room);PrefabUtility.RecordPrefabInstancePropertyModifications(room);}
            Configure(table.roomUnderstanding);var prefab=PrefabUtility.LoadPrefabContents(Launch);
            try{Configure(prefab.GetComponentInChildren<RaidersRoom>(true));PrefabUtility.SaveAsPrefabAsset(prefab,Launch);}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            EditorSceneManager.MarkSceneDirty(table.gameObject.scene);EditorSceneManager.SaveScene(table.gameObject.scene);AssetDatabase.SaveAssets();
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/12 - Validate enlarged MR")]
        public static void Run()
        {
            Guard();var log=new StringBuilder("Task 5 synthetic Edit Mode; native room containment replaced by a bounded fixture, not device MR.\n");int count=0;
            void Check(bool ok,string msg){if(!ok)throw new Exception(msg);count++;log.AppendLine("PASS "+msg);}
            bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.0001f;
            float normal=TabletopViewContract.MRBaselineScale(1),land=normal*TabletopViewContract.UsableLandSize;
            Check(Mathf.Abs(land-2.50998008f)<.00001f,"New square land derives from both doubled old axes: 2.509980 m");
            Check(Mathf.Abs(land*land/(1.5f*1.05f)-4)<.00001f,"Enlargement is four times old land area, not twice area");
            Check(Mathf.Abs(TabletopViewContract.MRBaselineScale(.6f)/normal-.6f)<.00001f,"Exposed previous-scale calibration applies exactly once");
            Check(Mathf.Approximately(TabletopViewContract.MRBaselineScale(float.NaN),normal),"Invalid calibration uses explicit calculated full-scale baseline");
            var fixture=new GameObject("Inactive Task5 synthetic MR");fixture.SetActive(false);
            var seam=typeof(RaidersRoom).GetField("containsFixture",BindingFlags.Static|BindingFlags.NonPublic);
            try
            {
                Transform Node(string n,Transform parent=null){var g=new GameObject(n);g.transform.SetParent(parent?parent:fixture.transform,false);return g.transform;}
                var mruk=fixture.AddComponent<MRUK>();var room=Node("Synthetic room").gameObject.AddComponent<MRUKRoom>();mruk.Rooms.Add(room);
                var roomBounds=new Bounds(new Vector3(0,2,0),new Vector3(16,4,16));
                seam.SetValue(null,(Func<MRUKRoom,Vector3,bool>)((r,p)=>r==room&&roomBounds.Contains(p)));
                var floor=Node("Synthetic floor",room.transform).gameObject.AddComponent<MRUKAnchor>();room.FloorAnchors.Add(floor);
                var anchor=Node("Synthetic furniture",room.transform).gameObject.AddComponent<MRUKAnchor>();anchor.transform.position=new Vector3(0,0,1.85f);
                Property(anchor,"Label",MRUKAnchor.SceneLabels.TABLE);Property(anchor,"VolumeBounds",(Bounds?)new Bounds(new Vector3(0,.375f,0),new Vector3(1.4f,.75f,1.4f)));room.Anchors.Add(anchor);
                var understanding=fixture.AddComponent<RaidersRoom>();understanding.mruk=mruk;
                var head=Node("Physical head");head.position=new Vector3(0,1.3f,0);
                Check(understanding.TryPlace(head,out var preview)&&understanding.PreviewKind==RaidersRoom.PlacementKind.Free,"Small scanned table offers explicit free MR instead of shrinking");
                Check(Mathf.Approximately(understanding.PreviewScale,normal)&&understanding.Status.Contains("Not attached"),"Free preview retains requested baseline and says it is not attached to furniture");
                Check(Mathf.Abs(preview.position.z-TabletopViewContract.OuterSize*normal*.5f-.3f)<.00001f,"New half-depth leaves near rim 30cm forward of seated head");
                Check(Mathf.Abs(head.position.y-preview.position.y-TabletopViewContract.RimHeight*normal-.5f)<.00001f,"Free normal rim remains 50cm below eyes");
                Check(preview.position.y+TabletopViewContract.TrayBottom*normal>.06f,"Normal tray underside clears synthetic floor");
                understanding.CancelPreview();Check(!understanding.HasPreview&&!understanding.Confirm(head),"Cancelled free preview cannot confirm");
                understanding.TryPlace(head,out preview);Check(understanding.Confirm(head)&&understanding.IsFree,"Explicit confirmation adopts free placement");
                Check(understanding.TryConfirmed(head,out var home,out float scale)&&Near(home.position,preview.position)&&Mathf.Approximately(scale,normal),"Free confirmed home and baseline round-trip");
                Check(!understanding.FitsConfirmed(home.position,home.rotation,scale),"Free placement is never reported as surface anchored");
                int version=understanding.ConfirmationVersion;understanding.TryPlace(head,out _,true);
                Check(understanding.IsFree&&!understanding.HasPreview&&understanding.ConfirmationVersion==version,"Failed strict-table request preserves current free placement");
                head.position=new Vector3(20,1.3f,0);Check(!understanding.TryConfirmed(head,out _,out _),"Leaving loaded room invalidates free placement for recovery");head.position=new Vector3(0,1.3f,0);
                understanding.previousMRReferenceScale=.6f;Check(!understanding.TryConfirmed(head,out _,out _),"Changed calibration requires fresh confirmation instead of compounding saved scale");understanding.previousMRReferenceScale=1;
                var table=fixture.AddComponent<RaidersTable>();var mode=fixture.AddComponent<RoomChoiceMenu>();var session=fixture.AddComponent<RaidersSession>();var launch=fixture.AddComponent<LaunchSequence>();var input=fixture.AddComponent<RaidersTableManipulation>();
                table.roomUnderstanding=understanding;table.roomMenu=mode;table.session=session;table.launch=launch;table.manipulation=input;input.table=table;launch.headset=head;
                table.presentation=Node("Virtual board");table.root=Node("Gameplay",table.presentation);table.virtualTable=Node("Tray",table.presentation).gameObject;table.terrainAssembly=Node("Terrain",table.presentation).gameObject;
                table.environmentMotion=Node("VR room");table.environmentMotion.position=new Vector3(3,0,4);table.humanPresentation=Node("Human seats");table.seats=Array.Empty<Transform>();
                var avatar=Node("Physical avatar",head);var hand=Node("Physical hand",head);var beforeAvatar=avatar.position;var beforeHand=hand.position;var roomPose=new Pose(table.environmentMotion.position,table.environmentMotion.rotation);
                Field(mode,"<HasSelection>k__BackingField",true);Field(mode,"<IsVirtual>k__BackingField",false);Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);
                table.ModeChosen();Check(table.Exploration&&Mathf.Approximately(table.RelativeZoom,1)&&Mathf.Approximately(table.NormalViewScale,normal),"Free MR starts at exactly its enlarged normal/minimum");
                float now=Time.unscaledTime+10;Vector3 l=new Vector3(-.2f,1,-2),r=new Vector3(.2f,1.3f,-2);
                void Step(bool lc=true,bool rc=true,bool valid=true){now+=.11f;input.ProcessHands(new RaidersTableManipulation.HandSample(valid,lc,l),new RaidersTableManipulation.HandSample(valid,rc,r),now);}
                Step(false,false);Step();Step();var acquired=table.presentation.position;
                Check(input.Dragging&&Near(acquired,home.position),"Both palm grabs acquire in empty air without a jump in enlarged MR");
                l+=Vector3.right*.2f;r+=Vector3.right*.2f;Step();Check(Near(table.presentation.position,acquired+Vector3.right*.2f),"Off-table two-hand translation works in free MR");
                Step(false,true);var stopped=table.presentation.position;l+=Vector3.right;Step(false,true);Check(Near(stopped,table.presentation.position),"One-hand movement after release cannot move MR board");
                Step();Step();Check(Near(stopped,table.presentation.position),"Regrab captures fresh MR baseline");
                r=l+Vector3.right*100;Step();Check(Mathf.Abs(table.RelativeZoom-6)<.00001f,"MR maximum is six times enlarged baseline");
                r-=Vector3.right*.1f;Step();Check(table.RelativeZoom<6&&table.RelativeZoom>5.9f,"MR upper clamp reverses immediately");
                r=l+Vector3.right*.08f;Step();Check(Mathf.Abs(table.RelativeZoom-1)<.00001f,"MR minimum forbids shrinking below enlarged baseline");
                r=l+Vector3.right*.09f;Step();Check(table.RelativeZoom>1,"MR lower clamp reverses immediately");
                Step(true,true,false);Check(!input.Dragging,"Tracking loss freezes enlarged MR");
                Check(Near(table.environmentMotion.position,roomPose.position)&&table.environmentMotion.rotation==roomPose.rotation&&table.environmentMotion.lossyScale==Vector3.one,"MR gesture leaves real-room substitute and VR environment transform unchanged");
                Check(Near(avatar.position,beforeAvatar)&&Near(hand.position,beforeHand)&&avatar.lossyScale==Vector3.one&&head.lossyScale==Vector3.one,"Avatar, physical hands and head remain aligned at unit scale");
                var moved=table.presentation.position;table.presentation.localScale=Vector3.one*normal*3;table.PrepareModeChange();
                Field(mode,"<IsVirtual>k__BackingField",true);table.ModeChosen();Check(table.NormalViewScale==1&&table.RelativeZoom==1,"VR keeps its existing normal baseline");
                Field(mode,"<IsVirtual>k__BackingField",false);table.ModeChosen();Check(Near(moved,table.presentation.position)&&Mathf.Abs(table.RelativeZoom-3)<.00001f&&table.Exploration,"MR round trip restores accepted free pose and relative zoom without double multiplication");
                Field(table,"mrZoom",.2f);table.ModeChosen();Check(Mathf.Abs(table.RelativeZoom-1)<.00001f,"Below-normal saved MR zoom clamps on restore");
                Field(table,"mrZoom",99f);table.ModeChosen();Check(Mathf.Abs(table.RelativeZoom-6)<.00001f,"Excessive saved MR zoom clamps on restore");
                head.rotation=Quaternion.Euler(0,63,0);head.position=new Vector3(1,1.2f,-1);table.ResetView();var reachable=TabletopViewMath.ReachablePose(head,normal,0);
                Check(Near(table.presentation.position,reachable.position)&&Quaternion.Angle(table.presentation.rotation,reachable.rotation)<.001f&&Mathf.Abs(table.RelativeZoom-1)<.00001f,"Free reset follows current seated head yaw, returns to normal, retains free mode");
                Check(table.Exploration&&!understanding.HasPreview,"Free reset does not mislabel placement as anchored");
                head.position=new Vector3(0,1.3f,0);head.rotation=Quaternion.identity;
                Property(anchor,"VolumeBounds",(Bounds?)new Bounds(new Vector3(0,.375f,0),new Vector3(5,.75f,5)));
                Check(!understanding.TryPlace(head,out _,true),"Wide table still rejected when enlarged rim is too near seated eyes");
                head.position=new Vector3(0,1.6f,0);
                Check(understanding.TryPlace(head,out preview,true)&&understanding.PreviewKind==RaidersRoom.PlacementKind.Surface,"Large clear scanned table offers full-size strict surface preview");
                Check(understanding.Confirm(head)&&!understanding.IsFree,"Surface confirmation explicitly changes constraint");table.ModeChosen();
                Check(!table.Exploration&&Mathf.Approximately(table.RelativeZoom,1),"Surface placement starts at same enlarged minimum");
                Check(Mathf.Abs(table.presentation.position.y+TabletopViewContract.TrayBottom*normal-.756f)<.00001f,"Surface tray underside is 6mm above 75cm tabletop");
                Check(table.AcceptView(table.presentation.position,table.presentation.rotation,normal),"Full enlarged footprint accepted on large table");
                Check(!table.AcceptView(table.presentation.position+Vector3.right*2,Quaternion.identity,normal),"Overhang rejected in surface-constrained mode");
                float two=normal*2;Check(!table.AcceptView(new Vector3(0,table.AnchoredOriginHeight(two),1.85f),Quaternion.identity,two),"Surface zoom refuses footprint overflow instead of calling it anchored");
                table.ToggleExploration();Check(table.Exploration&&understanding.IsFree,"Switch to free MR releases constraint without duplicating the board");
                Check(table.AcceptView(Vector3.one*2,Quaternion.Euler(0,40,0),normal*6),"Free MR permits full inspection range independently of table fit");
                table.ToggleExploration();Check(understanding.HasPreview&&table.Exploration,"Return to surface shows preview before changing accepted mode");table.ResetView();Check(!understanding.HasPreview&&table.Exploration,"Recenter cancels pending surface preview and retains free mode");
                Check(!RaidersRoom.FitsAnchor(anchor,Vector3.zero,Quaternion.identity,float.NaN),"Nonfinite scale cannot pass strict footprint validation");
                head.position=new Vector3(0,.4f,0);var low=TabletopViewMath.ReachablePose(head,normal,0);Check(Mathf.Abs(low.position.y+TabletopViewContract.TrayBottom*normal-.06f)<.00001f,"Very low eye height retains explicit 6cm free floor clearance");
                for(int i=0;i<4;i++){var seat=TabletopViewMath.SeatPosition(i,4,normal);Check(Mathf.Abs(new Vector2(seat.x,seat.z).magnitude-(1.3f*normal*.5f+.3f))<.00001f,"Human seat "+i+" uses enlarged normal half-depth, not zoom scale");}
                log.AppendLine("normal="+normal.ToString("F7")+" outer="+(normal*1.3f).ToString("F6")+" land="+land.ToString("F6")+" engine="+(normal*.148f).ToString("F6")+" maximum="+(normal*6).ToString("F6"));
            }
            finally{seam.SetValue(null,null);Object.DestroyImmediate(fixture);}
            var actual=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);
            Check(actual.roomUnderstanding.previousMRReferenceScale==1,"Saved scene explicitly exposes full-scale calculated reference");
            Check(actual.root.localScale==Vector3.one&&actual.terrainMount.localScale==Vector3.one&&actual.locomotives.All(t=>Mathf.Approximately(t.localScale.x,.01f)),"Corrected miniature proportions have no second MR multiplier");
            Check(!actual.launch.xrRigRoot.transform.IsChildOf(actual.presentation)&&!actual.session.avatars.trackingSpace.IsChildOf(actual.presentation)&&actual.humanPresentation.lossyScale==Vector3.one,"Physical tracking and both avatar branches remain outside miniature scale");
            float TextHeight(UnityEngine.UI.Text label,string value){var settings=label.GetGenerationSettings(label.rectTransform.rect.size);settings.generationExtents.y=0;return new TextGenerator().GetPreferredHeight(value,settings)/label.pixelsPerUnit;}
            var nav=actual.GetComponent<RaidersMenu>();
            Check(TextHeight(nav.message,"Free MR - not attached to furniture. Close BOTH hands anywhere to move, turn or resize. Recenter restores normal size. Try scanned table never shrinks to fit.")<=nav.message.rectTransform.rect.height,"MR view instructions fit actual menu font/feedback area");
            Check(TextHeight(actual.roomMenu.status,"Free MR preview. Not attached to furniture. Use your room again to confirm.")<=actual.roomMenu.status.rectTransform.rect.height,"Free-placement confirmation fits existing room status label");
            var prefab=PrefabUtility.LoadPrefabContents(Launch);
            try{Check(prefab.GetComponentInChildren<RaidersRoom>(true).previousMRReferenceScale==1,"Reusable launch prefab has matching exposed MR calibration");}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            log.AppendLine(count+" Task5 assertions passed. No native containment, passthrough, account, live gesture or headset comfort validation.");
            File.WriteAllText("Deliverables/DemeoRework/validation-task5.txt",log.ToString());Debug.Log(log.ToString());
        }
    }
}
