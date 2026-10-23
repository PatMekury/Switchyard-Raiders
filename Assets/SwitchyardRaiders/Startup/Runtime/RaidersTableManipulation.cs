using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    // Personal presentation only. No interactable, contact volume, network command or rig writer.
    public sealed class RaidersTableManipulation : MonoBehaviour
    {
        public enum GrabState { Ready, OneHand, Acquiring, Moving, AwaitOpen }
        public struct HandSample
        {
            public bool valid, closed;
            public Vector3 palm;
            public HandSample(bool valid,bool closed,Vector3 palm){this.valid=valid;this.closed=closed;this.palm=palm;}
        }
        public RaidersTable table;
        public OVRHand leftHand, rightHand;
        public HandGrabAPI leftPalm, rightPalm;
        public OVRVignette vignette;
        [Range(0,1)] public float comfort=.5f;
        public GrabState State { get; private set; } = GrabState.AwaitOpen;
        public bool Dragging => State==GrabState.Moving;
        public int GrabCount { get; private set; }
        public int InteractionEpoch { get; private set; }
        public const float AcquisitionSeconds=.10f, ReleaseGuardSeconds=.25f;
        private bool focused=true, paused;
        private float acquireAt, releaseUntil;
        private Vector3 previousLeft, previousRight, localPivot;

        // Query the classifier at the command boundary too: UI callbacks can precede LateUpdate.
        // One hand/pinch alone never locks menus. A candidate pair, active move and release guard
        // consume commands. Epochs cancel button presses begun before a table gesture.
        public bool BlocksGameplay => State==GrabState.Acquiring || Dragging || Time.unscaledTime<releaseUntil || PairIntent();
        private bool PairIntent()
        {
            if(!table || !table.CanManipulate || !focused || paused || State==GrabState.AwaitOpen ||
                (Application.isPlaying && (!OVRManager.hasInputFocus || !OVRManager.hasVrFocus)))return false;
            var l=Read(leftPalm,leftHand);var r=Read(rightPalm,rightHand);
            return l.valid && r.valid && l.closed && r.closed;
        }
        public static HandSample Read(HandGrabAPI api,OVRHand tracked)
        {
            var hand=api?api.Hand:null;
            if(!tracked || !tracked.IsTracked || tracked.IsSystemGestureInProgress ||
                tracked.HandConfidence!=OVRHand.TrackingConfidence.High || hand==null ||
                !hand.IsConnected || !hand.IsTrackedDataValid || !hand.IsHighConfidence ||
                !hand.GetRootPose(out _) || !hand.GetFingerIsHighConfidence(HandFinger.Middle) ||
                !hand.GetFingerIsHighConfidence(HandFinger.Ring) || !hand.GetFingerIsHighConfidence(HandFinger.Pinky))return default;
            // Official palm classifier (with curl hysteresis), never pinch strength.
            // Requiring all three ulnar fingers rejects thumb/index pinches. Opening any ends it.
            var fingers=api.HandPalmGrabbingFingers();
            return new HandSample(true,PalmClosed(fingers),api.GetPalmCenter());
        }
        public static bool PalmClosed(HandFingerFlags fingers)
        {
            var required=HandFingerFlags.Middle|HandFingerFlags.Ring|HandFingerFlags.Pinky;
            return (fingers&required)==required;
        }
        private void LateUpdate()
        {
            if(!OVRManager.hasInputFocus || !OVRManager.hasVrFocus){Suspend();return;}
            ProcessHands(Read(leftPalm,leftHand),Read(rightPalm,rightHand),Time.unscaledTime);
        }

        // Same path is exercised by deterministic Edit Mode fixtures. This does not simulate
        // tracking on the actual rig and is not evidence of live hand recognition.
        public void ProcessHands(HandSample left,HandSample right,float now)
        {
            bool allowed=table && table.CanManipulate && focused && !paused;
            if(!allowed || !left.valid || !right.valid || !TabletopViewMath.Finite(left.palm) || !TabletopViewMath.Finite(right.palm))
            {Stop(now,true);return;}
            GrabCount=(left.closed?1:0)+(right.closed?1:0);
            if(State==GrabState.AwaitOpen)
            {
                if(GrabCount==0)State=GrabState.Ready;
                return; // Both open after a tracking/focus/mode interruption prevents stale reentry.
            }
            if(GrabCount!=2)
            {
                Stop(now,false);State=GrabCount==1?GrabState.OneHand:GrabState.Ready;return;
            }
            if(State!=GrabState.Acquiring && !Dragging)
            {State=GrabState.Acquiring;InteractionEpoch++;acquireAt=now;previousLeft=left.palm;previousRight=right.palm;HoldTray(true);return;}
            if(State==GrabState.Acquiring)
            {
                if(Vector3.Distance(previousLeft,left.palm)>.025f || Vector3.Distance(previousRight,right.palm)>.025f ||
                    TabletopViewMath.PlanarSpan(left.palm,right.palm)<.08f)acquireAt=now;
                previousLeft=left.palm;previousRight=right.palm;
                if(now-acquireAt<AcquisitionSeconds)return;
                table.EnforceViewBounds();
                localPivot=table.presentation.InverseTransformPoint(TabletopViewMath.Pivot(table.presentation.position,
                    table.presentation.rotation,table.presentation.localScale.x,(left.palm+right.palm)*.5f));
                State=GrabState.Moving;return;
            }
            var view=table.presentation;
            var pivot=view.TransformPoint(localPivot);
            TabletopViewMath.SolveAtPivot(view.position,view.rotation,view.localScale.x,pivot,
                previousLeft,previousRight,left.palm,right.palm,table.NormalViewScale,table.MaximumViewScale,
                out var position,out var rotation,out float scale,out var translation,out var yaw);
            if(!table.roomMenu.IsVirtual && !table.Exploration)position.y=table.AnchoredOriginHeight(scale);
            bool accepted=table.AcceptView(position,rotation,scale);
            bool moving=accepted && (Vector3.Distance(view.position,position)>.0005f || Quaternion.Angle(view.rotation,rotation)>.05f || Mathf.Abs(view.localScale.x-scale)>.0005f);
            if(accepted)table.ApplyPersonalView(position,rotation,scale,pivot,translation,yaw);
            if(vignette){vignette.VignetteFieldOfView=Mathf.Lerp(110,65,comfort);vignette.enabled=moving && table.roomMenu.IsVirtual && comfort>0;}
            // Rebase at clamps, invalid span and rejected MR bounds. No overshoot dead travel.
            previousLeft=left.palm;previousRight=right.palm;
        }
        private void Stop(float now,bool requireOpen)
        {
            if(Dragging || State==GrabState.Acquiring){releaseUntil=now+ReleaseGuardSeconds;InteractionEpoch++;}
            State=requireOpen?GrabState.AwaitOpen:GrabState.Ready;
            if(requireOpen)GrabCount=0;
            if(vignette)vignette.enabled=false;
            HoldTray(false);
        }
        private void HoldTray(bool hold){if(table && table.roomMenu && table.roomMenu.follower)table.roomMenu.follower.HoldFor(this,hold);}
        public void Suspend()=>Stop(Time.unscaledTime,true);
        private void OnApplicationFocus(bool focus){focused=focus;if(!focus)Suspend();}
        private void OnApplicationPause(bool pause){paused=pause;if(pause)Suspend();}
        private void OnDisable()=>Suspend();
    }
}
