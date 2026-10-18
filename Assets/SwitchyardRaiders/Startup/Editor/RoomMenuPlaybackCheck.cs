using System;
using System.IO;
using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using System.Text;
using SwitchyardRaiders.Startup;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SwitchyardRaiders.Editor
{
    [InitializeOnLoad]
    public static class RoomMenuPlaybackCheck
    {
        private const string Key = "SR.RoomMenu.PlaybackCheck";
        private static int step;
        private static double next;
        private static PointableCanvas pointer;
        private static Pose pose;
        private static Vector3 heldPosition;
        static RoomMenuPlaybackCheck() { EditorApplication.update += Tick; }

        [MenuItem("Switchyard Raiders/Room Menu/Run desktop interaction check")]
        public static void Run() => StartCheck(false);
        public static void RunFullReload() => StartCheck(true);
        [MenuItem("Switchyard Raiders/Room Menu/Capture live hand alignment")]
        public static void CaptureHandAlignment()
        {
            var launch=Object.FindAnyObjectByType<LaunchSequence>();
            if (!EditorApplication.isPlaying || !launch || launch.IsDesktopPreview || !UnityEngine.XR.XRSettings.isDeviceActive)
                throw new InvalidOperationException("A running headset Play session is required; desktop poses cannot validate alignment.");
            var rig=launch.xrRigRoot.GetComponent<OVRCameraRig>();
            var report=new StringBuilder("Live hand alignment snapshot "+DateTime.Now.ToString("s")+"\n");
            report.AppendLine("Tracking origin: "+OVRManager.instance.trackingOriginType+"; tracking scale: "+rig.trackingSpace.lossyScale.ToString("F4"));
            report.AppendLine("Head world: "+rig.centerEyeAnchor.position.ToString("F4"));
            foreach(var visual in rig.GetComponentsInChildren<HandVisual>(true))
            {
                var hand=visual.Hand;
                bool valid=hand!=null && hand.IsTrackedDataValid && hand.GetRootPose(out _);
                report.AppendLine(visual.name+" tracked pose valid: "+valid);
                if(!valid) continue;
                hand.GetRootPose(out var root);
                report.AppendLine("  SDK wrist: "+root.position.ToString("F4")+"; rendered root: "+visual.Root.position.ToString("F4")+"; separation mm: "+(1000*Vector3.Distance(root.position,visual.Root.position)).ToString("F2"));
                var raw=hand.Handedness==Handedness.Left ? launch.leftHand:launch.rightHand;
                var source=((OVRSkeleton.IOVRSkeletonDataProvider)raw).GetSkeletonPoseData();
                var tracking=source.RootPose.ToOVRPose();
                Vector3 expected=rig.trackingSpace.TransformPoint(tracking.position);
                report.AppendLine("  Raw tracking wrist transformed to world: "+expected.ToString("F4")+"; SDK separation mm: "+(1000*Vector3.Distance(expected,root.position)).ToString("F2"));
            }
            foreach(var poke in rig.GetComponentsInChildren<PokeInteractor>(true))
            {
                var point=new SerializedObject(poke).FindProperty("_pointTransform").objectReferenceValue as Transform;
                var joint=point ? point.GetComponent<HandJoint>():null;
                report.AppendLine(poke.name+" point: "+(point ? point.position.ToString("F4"):"MISSING")+"; tracked joint: "+(joint ? joint.HandJointId.ToString():"MISSING")+"; configured fingertip offset: "+(joint ? joint.LocalPositionOffset.ToString("F4"):"MISSING"));
            }
            report.AppendLine("World-space agreement does not alone establish alignment with the passthrough image. Confirm both stationary hands in the headset.");
            Directory.CreateDirectory(RoomMenuSetup.Report);
            File.WriteAllText(RoomMenuSetup.Report+"/live-hand-alignment.txt",report.ToString());
            Debug.Log(report.ToString());
        }
        private static void StartCheck(bool fullReload)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before testing.");
            LaunchPerformanceSetup.UseDesktop();
            if (fullReload) EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            Directory.CreateDirectory(RoomMenuSetup.Report);
            File.WriteAllText(RoomMenuSetup.Report+"/playback-check.txt", "RUNNING\n");
            SessionState.SetBool(Key,true); step=0; next=0;
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.isPlaying=true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup < next) return;
            try
            {
                var menu=Object.FindAnyObjectByType<RoomChoiceMenu>();
                // Automated desktop checks may run while another app owns OS focus.
                // Simulate focus only in this test; headset focus gating stays unchanged.
                if (menu && menu.launch.IsDesktopPreview)
                    menu.launch.SendMessage("OnApplicationFocus",true,SendMessageOptions.RequireReceiver);
                if (!menu || !menu.IsOpen || menu.group.alpha < .999f)
                {
                    if (Time.realtimeSinceStartup > 35) throw new Exception("Opening/menu timed out.");
                    return;
                }
                switch(step)
                {
                    case 0:
                        Require(menu.launch.Phase == LaunchGate.Phase.Holding && !menu.launch.director.playableGraph.IsValid(), "Timeline did not release its graph.");
                        Require(!menu.HasSelection && !menu.virtualRoom.activeSelf, "New Play must reset room selection.");
                        ValidateLayout(menu);
                        ValidatePointerConnections(menu);
                        Require(Mathf.Abs(Vector3.Dot(menu.follower.transform.position-menu.launch.headset.position, menu.launch.headset.forward)-.65f) < .01f,"Menu distance wrong.");
                        Capture(menu,"menu-desktop.png");
                        BeginPointer(menu,false); break;
                    case 1: Send(PointerEventType.Select); break;
                    case 2: Send(PointerEventType.Unselect); break;
                    case 3:
                        Send(PointerEventType.Unhover);
                        Require(menu.HasSelection && !menu.IsVirtual, "SDK pointer did not select Real Room.");
                        BeginPointer(menu,true); break;
                    case 4: Send(PointerEventType.Select); break;
                    case 5: Send(PointerEventType.Unselect); break;
                    case 6:
                        Send(PointerEventType.Unhover);
                        Require(menu.IsVirtual && menu.virtualRoom.activeSelf && menu.launch.passthrough.hidden,"Virtual room not visible / passthrough not hidden.");
                        Capture(menu,"virtual-room-desktop.png"); BeginPointer(menu,false); break;
                    case 7: Send(PointerEventType.Select); break;
                    case 8: Send(PointerEventType.Unselect); break;
                    case 9:
                        Send(PointerEventType.Unhover);
                        Require(!menu.IsVirtual && !menu.virtualRoom.activeSelf && !menu.launch.passthrough.hidden,"Return to real room failed.");
                        Require(menu.realButton.interactable && menu.virtualButton.interactable,"Buttons stayed locked after transition.");
                        heldPosition = menu.follower.transform.position;
                        menu.follower.InteractionHeld = true;
                        menu.launch.headset.rotation = Quaternion.Euler(0,90,0);
                        break;
                    case 10:
                        Require(Vector3.Distance(menu.follower.transform.position,heldPosition) < .001f,"Menu moved during interaction hold.");
                        menu.follower.InteractionHeld = false;
                        break;
                    case 11:
                        Require(Vector3.Distance(menu.follower.transform.position,heldPosition) > .05f,"Menu failed to follow after a large head turn.");
                        next=EditorApplication.timeSinceStartup+2.5; step++; return;
                    case 12:
                        ValidateLayout(menu);
                        Capture(menu,"menu-turned-desktop.png");
                        var wanted=menu.launch.headset.position+Vector3.right*.65f+Vector3.up*menu.follower.heightOffset;
                        Require(!menu.follower.InteractionHeld,"Menu is still held after pointer release.");
                        Require(Vector3.Distance(menu.follower.transform.position,wanted)<.05f,"Menu did not settle in front of the turned head: panel="+menu.follower.transform.position+" target="+wanted+" head="+menu.launch.headset.eulerAngles+" panel rotation="+menu.follower.transform.eulerAngles);
                        menu.launch.headset.rotation = Quaternion.identity;
                        menu.follower.Snap(menu.launch.headset);
                        File.WriteAllText(RoomMenuSetup.Report+"/playback-check.txt", "PASS: fresh Play reset, Timeline completion and graph disposal, .65 m menu placement; text fits its artwork parent before and after a head turn; Poke/Ray surfaces forward to their PointableCanvas; synthetic Meta PointableCanvas Hover/Select/Unselect events reached both buttons; real -> virtual -> real transition, final buttons enabled; panel holds steady during interaction and follows after a large head turn.\nThis verifies SDK event routing in desktop Play, not physical hand tracking or headset FPS.\nTime: "+DateTime.Now.ToString("s"));
                        SessionState.SetBool(Key,false);
                        EditorApplication.isPlaying=false;
                        Debug.Log("PASS: room menu desktop interaction check."); return;
                }
                step++; next=EditorApplication.timeSinceStartup+.65;
            }
            catch(Exception e)
            {
                File.WriteAllText(RoomMenuSetup.Report+"/playback-check.txt", "FAIL step "+step+": "+e);
                SessionState.SetBool(Key,false); EditorApplication.isPlaying=false; Debug.LogException(e);
            }
        }

        private static void BeginPointer(RoomChoiceMenu menu, bool virtualChoice)
        {
            pointer=menu.GetComponentsInChildren<PointableCanvas>().First(p=>virtualChoice ? p.GetComponent<RayInteractable>() != null : p.GetComponent<PokeInteractable>() != null);
            var button=virtualChoice ? menu.virtualButton : menu.realButton;
            pose=new Pose(button.transform.position,button.transform.rotation);
            Send(PointerEventType.Hover);
        }
        private static void Send(PointerEventType type) => pointer.ProcessPointerEvent(new PointerEvent(73519,type,pose));
        private static void ValidateLayout(RoomChoiceMenu menu)
        {
            foreach (var label in menu.canvas.GetComponentsInChildren<UnityEngine.UI.Text>())
            {
                var parent=label.transform.parent as RectTransform;
                Require(parent && parent.GetComponent<UnityEngine.UI.Image>(), label.name+" has no artwork parent.");
                var corners=new Vector3[4]; label.rectTransform.GetWorldCorners(corners);
                foreach(var corner in corners)
                    Require(parent.rect.Contains((Vector2)parent.InverseTransformPoint(corner)),label.name+" extends outside its artwork.");
                Require(label.preferredHeight <= label.rectTransform.rect.height+1,label.name+" is vertically clipped.");
                Require(label.canvas==menu.canvas,label.name+" uses a different canvas.");
            }
        }
        private static void ValidatePointerConnections(RoomChoiceMenu menu)
        {
            foreach(var canvas in menu.GetComponentsInChildren<PointableCanvas>())
            {
                Component surface=canvas.GetComponent<PokeInteractable>();
                if (!surface) surface=canvas.GetComponent<RayInteractable>();
                Require(surface,"Missing interaction surface.");
                var field=new SerializedObject(surface).FindProperty("_pointableElement");
                Require(field!=null && field.objectReferenceValue==canvas,"Interaction surface is not connected to its canvas.");
            }
        }
        private static void Require(bool value,string message) { if(!value) throw new Exception(message); }
        private static void Capture(RoomChoiceMenu menu,string file)
        {
            var camera=menu.launch.headset.GetComponent<Camera>();
            var target=RenderTexture.GetTemporary(1280,960,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Default,4);
            var previous=camera.targetTexture; var active=RenderTexture.active;
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var texture=new Texture2D(1280,960,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1280,960),0,0); texture.Apply();
            File.WriteAllBytes(RoomMenuSetup.Report+"/"+file,texture.EncodeToPNG());
            camera.targetTexture=previous; RenderTexture.active=active;
            Object.DestroyImmediate(texture); RenderTexture.ReleaseTemporary(target);
        }
    }
}
