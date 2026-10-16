using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.XR;

namespace SwitchyardRaiders.Startup
{
    [DefaultExecutionOrder(-10000)]
    public sealed class LaunchSequence : MonoBehaviour
    {
        public PlayableDirector director;
        public LaunchPresentation presentation;
        public Transform titleAnchor;
        public Transform headset;
        public GameObject xrRigRoot;
        public GameObject desktopPreviewRoot;
        public Transform desktopPreviewHead;
        public OVRPassthroughLayer passthrough;
        public OVRHand leftHand;
        public OVRHand rightHand;
        [Tooltip("Editor only. Turn off for Quest Link / XR Simulator. Android never uses this preview.")]
        public bool desktopPreviewInEditor = true;
        [Min(0.5f)] public float titleDistance = 1.5f;
        [Range(0f, 20f)] public float titleDownAngle = 8f;
        [Tooltip("Connect table setup / the next screen here. Fires once after the opening finishes or is skipped.")]
        public UnityEvent onTitleReady = new();
        public LaunchGate.Phase Phase => gate.Current;
        public bool IsDesktopPreview => preview;
        public bool HasRoomFrame => roomReady;
        private readonly LaunchGate gate = new();
        private const string SeenKey = "SwitchyardRaiders.OpeningSeen.v1";
        private bool preview, roomReady, applicationPaused, applicationFocused = true;
        private bool returningPlayer, leftReleased, rightReleased, suspended, reportedTimeout;
        private float waitingTime;

        private void Awake()
        {
#if UNITY_EDITOR
            preview = desktopPreviewInEditor && !XRSettings.isDeviceActive;
#endif
            if (desktopPreviewRoot != null) desktopPreviewRoot.SetActive(preview);
            if (preview)
            {
                if (xrRigRoot != null) xrRigRoot.SetActive(false);
                headset = desktopPreviewHead;
                roomReady = true;
                Debug.Log("Switchyard Raiders: desktop timing preview; the solid background is not passthrough.", this);
            }
            if (passthrough != null) passthrough.passthroughLayerResumed.AddListener(OnRoomFrame);
            returningPlayer = PlayerPrefs.GetInt(SeenKey, 0) == 1;
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            presentation.opacity = 0f;
            presentation.Apply();
        }

        private void OnDestroy()
        {
            if (passthrough != null) passthrough.passthroughLayerResumed.RemoveListener(OnRoomFrame);
        }
        private void OnRoomFrame(OVRPassthroughLayer layer) => roomReady = true;
        private void OnApplicationPause(bool paused) => applicationPaused = paused;
        private void OnApplicationFocus(bool focused) => applicationFocused = focused;

        private void Update()
        {
            bool focused = !applicationPaused && (preview ? applicationFocused : OVRManager.hasInputFocus);
            if (gate.Current == LaunchGate.Phase.WaitingForRoom)
            {
                waitingTime += Time.unscaledDeltaTime;
                if (!roomReady && waitingTime > 10f && !reportedTimeout)
                {
                    reportedTimeout = true;
                    Debug.LogWarning("Opening is waiting for Meta passthrough to present its first frame. Check XR initialization and passthrough support; the title will not mask a failed room launch.", this);
                }
                bool ready = roomReady && (preview || (OVRManager.isHmdPresent && OVRManager.tracker.isPositionTracked));
                if (gate.AdvanceRoom(ready, focused, Time.unscaledDeltaTime)) BeginTitle();
                return;
            }
            if (gate.Current != LaunchGate.Phase.Playing) return;
            if (!focused)
            {
                if (!suspended) { director.Pause(); suspended = true; }
                leftReleased = rightReleased = false;
                return;
            }
            if (suspended) { director.Resume(); suspended = false; }
            if (director.time >= director.duration - 0.025) { Complete(); return; }
            // A held pinch, system gesture or reacquired hand must never skip the opening.
            if (returningPlayer && director.time > 0.45 &&
                (FreshPinch(leftHand, ref leftReleased) | FreshPinch(rightHand, ref rightReleased))) Skip();
        }

        private void BeginTitle()
        {
            Vector3 forward = Vector3.ProjectOnPlane(headset.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;
            Vector3 position = headset.position + forward * titleDistance;
            position.y -= Mathf.Tan(titleDownAngle * Mathf.Deg2Rad) * titleDistance;
            titleAnchor.SetPositionAndRotation(position, Quaternion.LookRotation(position - headset.position, Vector3.up));
            director.time = 0;
            director.Play();
        }

        private static bool FreshPinch(OVRHand hand, ref bool released)
        {
            if (hand == null || !hand.IsDataHighConfidence || hand.IsSystemGestureInProgress ||
                hand.GetFingerConfidence(OVRHand.HandFinger.Index) != OVRHand.TrackingConfidence.High)
            { released = false; return false; }
            bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            if (!pinching) { released = true; return false; }
            bool fresh = released;
            released = false;
            return fresh;
        }

        public void Skip()
        {
            if (!returningPlayer || gate.Current != LaunchGate.Phase.Playing || suspended) return;
            director.time = director.duration;
            director.Evaluate();
            Complete();
        }

        private void Complete()
        {
            if (!gate.Complete()) return;
            director.Pause();
            presentation.opacity = 1f;
            presentation.settle = 1f;
            presentation.verticalOffset = 0f;
            presentation.sweep = 2f;
            presentation.Apply();
            if (!preview) { PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save(); }
            onTitleReady.Invoke();
        }
    }
}
