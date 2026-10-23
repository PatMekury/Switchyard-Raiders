using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Oculus.Avatar2;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    public static class AvatarTask4Checks
    {
        const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Field(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
        static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
        sealed class TestHand:IHand
        {
            public Pose root;public bool valid=true,confidence=true,joints=true;public float handScale=1;
            public Handedness handedness;public Handedness Handedness=>handedness;
            public bool IsConnected=>valid;public bool IsHighConfidence=>confidence;public bool IsDominantHand=>false;public float Scale=>handScale;
            public bool IsPointerPoseValid=>valid;public bool IsTrackedDataValid=>valid;public int CurrentDataVersion=>1;
            public event Action WhenHandUpdated {add{}remove{}}
            public bool GetFingerIsPinching(HandFinger f)=>false;public bool GetIndexFingerIsPinching()=>false;public bool GetFingerIsHighConfidence(HandFinger f)=>confidence;public float GetFingerPinchStrength(HandFinger f)=>0;
            public bool GetRootPose(out Pose p){p=root;return valid;}
            public bool GetPointerPose(out Pose p)=>GetRootPose(out p);
            public bool GetJointPose(HandJointId id,out Pose p)=>GetRootPose(out p);
            public bool GetJointPoseLocal(HandJointId id,out Pose p)=>GetRootPose(out p);
            public bool GetJointPoseFromWrist(HandJointId id,out Pose p)=>GetRootPose(out p);
            public bool GetPalmPoseLocal(out Pose p)=>GetRootPose(out p);
            public bool GetJointPosesLocal(out ReadOnlyHandJointPoses p){p=new ReadOnlyHandJointPoses(Enumerable.Repeat(new Pose(Vector3.zero,Quaternion.Euler(10,20,30)),30).ToArray());return joints;}
            public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses p)=>GetJointPosesLocal(out p);
        }
        sealed class TestHmd:IHmd
        {
            public Pose pose;public bool valid=true;public event Action WhenUpdated{add{}remove{}}
            public bool TryGetRootPose(out Pose p){p=pose;return valid;}
        }
        [MenuItem("Switchyard Raiders/Demeo Rework/10 - Validate local avatar lifecycle")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || !Application.dataPath.EndsWith("Switchyard Raiders/Assets"))throw new Exception("Correct project/Edit Mode required.");
            int count=0;var log=new StringBuilder("Task 4 synthetic Edit Mode tests. No account authentication, native avatar rendering, Photon session or live tracking.\n");
            void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;log.AppendLine("PASS "+message);}
            bool Near(Vector3 a,Vector3 b)=>Vector3.Distance(a,b)<.00001f;
            var first=RaidersAvatarEntity.CreationFor(true);var third=RaidersAvatarEntity.CreationFor(false);
            Check(first.renderFilters.viewFlags==CAPI.ovrAvatar2EntityViewFlags.FirstPerson,"Local entity loads first-person-only representation");
            Check(first.renderFilters.subMeshInclusionFlags==(CAPI.ovrAvatar2EntitySubMeshInclusionFlags.Body|CAPI.ovrAvatar2EntitySubMeshInclusionFlags.Outfit),"Local load excludes head, face, eyes, hair and head accessories");
            Check(third.renderFilters.viewFlags==CAPI.ovrAvatar2EntityViewFlags.ThirdPerson,"Remote entity remains third-person");
            Check((first.features&CAPI.ovrAvatar2EntityFeatures.UseDefaultModel)==0&&(third.features&CAPI.ovrAvatar2EntityFeatures.UseDefaultModel)==0,"No SDK default/preset can be presented as authenticated account avatar");
            var fixture=new GameObject("Task4 inactive lifecycle fixture");fixture.SetActive(false);
            try
            {
                Transform Node(string name){var go=new GameObject(name);go.transform.SetParent(fixture.transform,false);return go.transform;}
                var service=fixture.AddComponent<RaidersAvatars>();var session=fixture.AddComponent<RaidersSession>();service.session=session;session.avatars=service;
                service.playerFallbacks=Array.Empty<GameObject>();service.playerSeats=Array.Empty<Transform>();
                var handNode=Node("Tracking stays active");handNode.gameObject.SetActive(true);var renderer=handNode.gameObject.AddComponent<MeshRenderer>();service.outlineRenderers=new Renderer[]{renderer};
                var other=Node("UI cursor");var cursor=other.gameObject.AddComponent<MeshRenderer>();
                service.SetOutlineHidden(true);Check(renderer.forceRenderingOff&&renderer.enabled&&handNode.gameObject.activeSelf,"Avatar swap hides renderer only, preserving enabled tracking/input parent");
                Check(!cursor.forceRenderingOff,"UI cursor is not hidden with outline hands");
                service.SetOutlineHidden(false);Check(!renderer.forceRenderingOff,"Menu restores the prior outline rendering state");
                renderer.forceRenderingOff=true;service.SetOutlineHidden(true);service.SetOutlineHidden(false);Check(renderer.forceRenderingOff,"Visual restoration preserves a pre-existing render suppression flag");renderer.forceRenderingOff=false;
                int auth=0,creation=0;
                Field(service,"identityFixture",(Func<Task>)(()=>{auth++;return Task.CompletedTask;}));
                Field(service,"createFixture",(Action)(()=>creation++));
                Field(session,"<IsLocal>k__BackingField",true);
                foreach(var phase in new[]{RaidersSessionPhase.Idle,RaidersSessionPhase.Ready,RaidersSessionPhase.Countdown})
                {Field(session,"<Phase>k__BackingField",phase);Call(service,"SessionChanged");}
                Check(auth==0&&creation==0&&service.Presentation==RaidersAvatars.LocalPresentation.Menu,"Menu/readiness/countdown do not replace menu hands");
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);Call(service,"SessionChanged");
                Check(auth==1&&creation==1&&service.Presentation==RaidersAvatars.LocalPresentation.Loading,"Solo gameplay requests legitimate identity path and local presentation independently of transport");
                Check(Get(session,"runner")==null,"Solo avatar lifecycle creates no Photon runner");
                Call(service,"SessionChanged");Check(auth==1&&creation==1,"Repeated session events cannot create duplicate local avatars");
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Finished);Call(service,"SessionChanged");Check(service.Presentation==RaidersAvatars.LocalPresentation.Menu&&!renderer.forceRenderingOff,"Gameplay exit restores menu representation");
                Field(session,"<IsLocal>k__BackingField",false);Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);Call(service,"SessionChanged");
                Check(auth==2&&creation==2,"Online gameplay shares local presentation path");
                Call(service,"ClearPresentation");
                Field(service,"identityFixture",(Func<Task>)(()=>Task.FromException(new InvalidOperationException("Synthetic identity unavailable"))));
                Call(service,"SessionChanged");Check(service.Presentation==RaidersAvatars.LocalPresentation.Fallback&&service.CanRetry&&service.Failure.Contains("Synthetic"),"Identity failure exposes explicit fallback status and retry");
                Check(service.Status.Contains("temporary hands")&&!renderer.forceRenderingOff,"Failure never silently labels outline hands as an authenticated avatar");
                Field(service,"identityFixture",(Func<Task>)(()=>Task.CompletedTask));service.RetryLocalAvatar();
                Check(service.Presentation==RaidersAvatars.LocalPresentation.Loading&&creation==3,"Retry restarts local loading without a network route");
                Call(service,"ClearPresentation");
                var pending=new TaskCompletionSource<bool>();Field(service,"identityFixture",(Func<Task>)(()=>pending.Task));Call(service,"SessionChanged");int ticket=(int)Get(service,"generation");
                Check(service.Presentation==RaidersAvatars.LocalPresentation.Identity,"Pending identity retains temporary visuals with explicit loading status");
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Idle);Call(service,"SessionChanged");
                Check(!(bool)Call(service,"Current",ticket),"Exit invalidates delayed identity completion before avatar creation");
                pending.SetResult(true);
                Check(((byte[])Get(service,"buffer")).Length==0,"Cleanup preserves non-null empty stream buffer");

                var space=Node("Physical tracking space");space.position=new Vector3(3,.2f,-4);space.rotation=Quaternion.Euler(0,67,0);
                service.trackingSpace=space;var input=fixture.AddComponent<RaidersAvatarInput>();
                Pose World(Vector3 p)=>new Pose(space.TransformPoint(p),space.rotation*Quaternion.Euler(5,12,3));
                var left=new TestHand{root=World(new Vector3(-.25f,1.1f,.4f))};var right=new TestHand{handedness=Handedness.Right,root=World(new Vector3(.25f,1.3f,.5f)),handScale=1.05f};var hmd=new TestHmd{pose=World(new Vector3(0,1.6f,0))};
                input.Configure(left,right,hmd,space);Check(input.TrackingReady,"Valid ISDK head and both full joint sets permit avatar visuals");
                Check(input.GetInputTrackingState(out var state)&&state.headsetActive&&state.leftControllerActive&&state.rightControllerActive,"Existing ISDK providers feed all avatar tracking inputs");
                var localHead=RaidersAvatarInput.InTrackingSpace(space,hmd.pose);Check(Near(localHead.position,new Vector3(0,1.6f,0))&&Quaternion.Angle(localHead.rotation,Quaternion.Euler(5,12,3))<.001f,"World poses convert once into rotated tracking-local space");
                var hands=new OvrAvatarTrackingHandsState();Check(input.GetHandData(hands)&&hands.isTrackedLeft&&hands.isTrackedRight,"Hand delegate supplies both tracked hands");

                Check(Mathf.Abs(hands.wristPosLeft.position.z+.4f)<.00001f&&Mathf.Abs(hands.wristPosLeft.position.x+.25f)<.00001f,"OpenXR wrist uses tracking-local metres with official avatar root Z conversion");
                Check(Mathf.Abs(hands.boneRotations[0].w-1)<.00001f&&Mathf.Abs(hands.boneRotations[17].w-1)<.00001f,"OpenXR absent thumb-base joints map to identity for both hands");
                var joint=Quaternion.Euler(10,20,30);
                var target=new HandMirroring.HandSpace(Vector3.left,Vector3.up,Vector3.forward);
                var expectedLeft=HandMirroring.TransformRotation(joint,HandTranslationUtils.openXRHands[Handedness.Left],target);
                var expectedRight=HandMirroring.TransformRotation(joint,HandTranslationUtils.openXRHands[Handedness.Right],target);
                Check(Mathf.Abs(hands.boneRotations[1].y-expectedLeft.y)<.00001f&&Mathf.Abs(hands.boneRotations[18].z-expectedRight.z)<.00001f,"OpenXR finger rotations use official per-hand coordinate bases");

                Check(Mathf.Abs(hands.handScaleRight-1.05f)<.00001f,"Physical hand scale is independent of tabletop zoom");
                left.confidence=false;input.GetHandData(hands);Check(!input.TrackingReady&&!hands.isConfidentLeft&&hands.isTrackedRight,"Confidence loss suppresses avatar readiness without disabling providers");
                left.confidence=true;right.joints=false;input.GetHandData(hands);Check(!input.TrackingReady&&!hands.isTrackedRight,"Missing hand joints cannot trigger successful visual replacement");
                right.joints=true;hmd.valid=false;Check(!input.TrackingReady,"Head tracking loss suppresses avatar visuals");hmd.valid=true;
                var view=Node("Miniature");var room=Node("Rigid room");var before=RaidersAvatarInput.InTrackingSpace(space,left.root);
                foreach(float zoom in new[]{1f,3f,6f}){view.localScale=Vector3.one*zoom;view.SetPositionAndRotation(new Vector3(9,2,4),Quaternion.Euler(0,120,0));room.SetPositionAndRotation(new Vector3(2,1,3),Quaternion.Euler(0,30,0));Check(Near(RaidersAvatarInput.InTrackingSpace(space,left.root).position,before.position)&&space.lossyScale==Vector3.one,"Table/room motion at "+zoom+"x does not alter physical avatar space");}
                var fake=Node("Synthetic loaded local entity").gameObject.AddComponent<RaidersAvatarEntity>();fake.local=true;
                Field(service,"localAvatar",fake);Field(service,"input",input);Field(service,"attempted",true);
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);Field(session,"<IsLocal>k__BackingField",true);
                Call(fake,"OnUserAvatarLoaded");Call(service,"Update");Check(service.Presentation==RaidersAvatars.LocalPresentation.Ready&&renderer.forceRenderingOff,"Only account-loaded callback plus valid tracking switches visual ownership");
                right.valid=false;Call(service,"Update");Check(service.Presentation==RaidersAvatars.LocalPresentation.TrackingPaused&&!renderer.forceRenderingOff,"Tracking loss restores temporary hand visuals with visible status");
                right.valid=true;Call(service,"Update");Check(service.Presentation==RaidersAvatars.LocalPresentation.Ready&&renderer.forceRenderingOff,"Tracking recovery restores avatar visuals without rebuilding rig");
                Call(fake,"Fail","Synthetic CDN failure");Call(service,"Update");Check(service.CanRetry&&!renderer.forceRenderingOff&&service.Failure=="Synthetic CDN failure","Native-load failure path restores fallback and retry");
                Call(service,"OnDisable");Check(!renderer.forceRenderingOff&&Get(service,"localAvatar")==null,"Disable clears local avatar and restores visuals");
                Check(((byte[])Get(service,"buffer"))!=null,"Disable never nulls stream buffer");
                // Inactive fixture entities never initialize the native SDK or authenticate these
                // arbitrary test identifiers. These checks cover ownership/placement/cleanup only.
                var seat0=Node("Synthetic seat 0");var seat1=Node("Synthetic seat 1");
                service.playerSeats=new[]{seat0,seat1};service.playerFallbacks=new GameObject[2];
                Field(service,"manager",Node("Synthetic service sentinel").gameObject);
                Field(session,"<Phase>k__BackingField",RaidersSessionPhase.Playing);
                ((int[])Get(session,"occupied"))[1]=1;
                var remotes=(RaidersAvatarEntity[])Get(service,"remoteAvatars");
                var payload=new byte[]{1};
                Call(service,"Receive",1,123UL,payload,new Vector3(1,0,2),35f);
                Check(remotes[1]==null,"Solo rejects remote-avatar packets; simulated slots stay simulated");
                Field(session,"<IsLocal>k__BackingField",false);
                Call(service,"Receive",0,123UL,payload,Vector3.zero,0f);
                Call(service,"Receive",1,0UL,payload,Vector3.zero,0f);
                Call(service,"Receive",1,123UL,Array.Empty<byte>(),Vector3.zero,0f);
                Call(service,"Receive",-1,123UL,payload,Vector3.zero,0f);
                Check(remotes.All(r=>r==null),"Own slot, zero identity, empty stream and invalid slot cannot create remote entities");
                var origin=new Vector3(1,0,2);var inverseYaw=Quaternion.Euler(0,-35,0);
                Call(service,"Receive",1,123UL,payload,origin,35f);var remote=remotes[1];
                Check(remote&&!remote.local&&remote.tracking==null&&remote.transform.parent==seat1,"Remote entity belongs to its human seat and has no local input driver");
                Check(Near(remote.transform.localPosition,inverseYaw*(-origin))&&Quaternion.Angle(remote.transform.localRotation,inverseYaw)<.001f&&remote.transform.lossyScale==Vector3.one,"Remote origin/yaw normalization preserves human metres");
                Call(service,"Receive",1,123UL,payload,origin,35f);Check(remote==remotes[1],"Repeated same-user stream reuses remote entity");
                Call(service,"Receive",1,456UL,payload,origin,35f);Check(!remote&&remotes[1]&&remotes[1].userId==456UL,"Slot ownership change releases previous remote entity");
                ((int[])Get(session,"occupied"))[1]=0;Call(service,"UpdateMarkers");Check(!remotes[1],"Vacated slot releases remote entity");
                ((int[])Get(session,"occupied"))[1]=1;Call(service,"Receive",1,123UL,payload,origin,35f);
                var borrowed=(GameObject)Get(service,"manager");Call(service,"ClearPresentation");
                Check(remotes.All(r=>!r)&&borrowed,"Match cleanup removes remote entities while retaining borrowed service");

            }
            finally{Object.DestroyImmediate(fixture);}
            var table=Object.FindAnyObjectByType<RaidersTable>(FindObjectsInactive.Include);var actual=table.session.avatars;
            Check(actual.leftHandSource&&actual.rightHandSource&&actual.hmdSource,"Scene binds existing ISDK hands and HMD");
            Check(actual.leftHandSource.name=="LeftHand"&&actual.rightHandSource.name=="RightHand","Correct authored handedness bound");
            Check(actual.outlineRenderers.Length==4&&actual.outlineRenderers.All(r=>r&&!r.name.Contains("Cursor")&&!r.name.Contains("Vignette")&&!r.name.Contains("Arrow")),"Exactly four OVR/OpenXR hand mesh renderers gated, excluding UI and vignette");
            Check(actual.managerPrefab.GetComponentsInChildren<OvrAvatarManager>(true).Length==1&&actual.managerPrefab.GetComponentsInChildren<OvrAvatarInputManager>(true).Length==1,"Existing reusable service has one avatar manager and one input manager");
            Check(!actual.trackingSpace.IsChildOf(table.presentation)&&!actual.trackingSpace.IsChildOf(table.environmentMotion)&&actual.trackingSpace.lossyScale==Vector3.one,"Local avatar tracking root is human scale outside table and room motion");
            Check(actual.playerSeats.All(s=>!s.IsChildOf(table.presentation)&&s.lossyScale==Vector3.one),"Remote avatars remain in unit-scale human seats");
            Check(table.manipulation.leftPalm&&table.manipulation.rightPalm,"Two-hand palm classifiers retained");
            Check(Object.FindObjectsByType<OVRCameraRig>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Scene still has exactly one XR rig");
            Check(Object.FindObjectsByType<RaidersSession>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Scene still has exactly one session owner");
            Check(Object.FindObjectsByType<OvrAvatarManager>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"No avatar manager instantiated during Edit Mode validation");
            var menu=actual.GetComponent<RaidersMenu>();var textGenerator=new TextGenerator();
            var footerSettings=menu.detail.GetGenerationSettings(menu.detail.rectTransform.rect.size);footerSettings.generationExtents.y=0;
            float footerHeight=textGenerator.GetPreferredHeight("SIMULATED P4   TEAM 1 0/6   TEAM 2 0/6\nAvatar unavailable - temporary hands; Table view > Avatar",footerSettings)/menu.detail.pixelsPerUnit;
            Check(footerHeight<=menu.detail.rectTransform.rect.height,"Actual menu font fits score and explicit fallback status in footer ("+footerHeight+" px)");
            var messageSettings=menu.message.GetGenerationSettings(menu.message.rectTransform.rect.size);messageSettings.generationExtents.y=0;
            float failureHeight=textGenerator.GetPreferredHeight("Avatar unavailable. Temporary hand visuals remain.\nThis account is not entitled to the app. Check release-channel or authorized test-user access.",messageSettings)/menu.message.pixelsPerUnit;
            Check(failureHeight<=menu.message.rectTransform.rect.height,"Account failure explanation fits existing feedback area ("+failureHeight+" px)");
            var prefab=PrefabUtility.LoadPrefabContents(AvatarTask4Setup.Launch);
            try{var a=prefab.GetComponentInChildren<RaidersAvatars>(true);Check(a.leftHandSource&&a.rightHandSource&&a.hmdSource&&a.outlineRenderers.Length==4,"Reusable launch prefab matches scene avatar bindings");}
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            log.AppendLine(count+" Task4 assertions passed. Native first-person appearance, account/CDN success and live interaction remain unverified.");
            File.WriteAllText("Deliverables/DemeoRework/validation-task4.txt",log.ToString());Debug.Log(log.ToString());
        }
    }
}
