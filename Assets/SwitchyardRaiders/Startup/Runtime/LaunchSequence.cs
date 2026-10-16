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
        public bool desktopPreviewInEditor;
        [Tooltip("Standalone Quest only. Requested once if the runtime advertises support; otherwise its default is kept.")]
        public float preferredRefreshRate = 90f;
        [Min(0.5f)] public float titleDistance = 1.5f;
        [Range(0f, 20f)] public float titleDownAngle = 8f;
        [Tooltip("Connect table setup / the next screen here. Fires once after the opening finishes or is skipped.")]
        public UnityEvent onTitleReady = new();
        public LaunchGate.Phase Phase => gate.Current;
        public bool IsDesktopPreview => preview;
        public bool HasRoomFrame => roomReady;
        private LaunchGate gate = new();
        private const string SeenKey = "SwitchyardRaiders.OpeningSeen.v1";
        private bool preview, roomReady, applicationPaused, applicationFocused = true;
        private bool returningPlayer, leftReleased, rightReleased, suspended, reportedTimeout;
        private float waitingTime;
#if UNITY_EDITOR
        private GUIStyle statusStyle;
#endif

        private void Awake()
        {
            gate = new LaunchGate();
            roomReady = applicationPaused = suspended = reportedTimeout = false;
            leftReleased = rightReleased = false;
            applicationFocused = true;
            waitingTime = 0f;
#if UNITY_EDITOR
            preview = desktopPreviewInEditor;
#endif
            if (desktopPreviewRoot != null) desktopPreviewRoot.SetActive(preview);
            if (xrRigRoot != null) xrRigRoot.SetActive(!preview);
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
            presentation.ResetForPlayback();
        }

        private void OnDestroy()
        {
            if (passthrough != null) passthrough.passthroughLayerResumed.RemoveListener(OnRoomFrame);
        }
        private void OnRoomFrame(OVRPassthroughLayer layer)
        {
            roomReady = true;
            Debug.Log("Switchyard Raiders: Meta passthrough presented its first frame.", this);
        }
        private void OnApplicationPause(bool paused) => applicationPaused = paused;
        private void OnApplicationFocus(bool focused) => applicationFocused = focused;

        private void Update()
        {
            bool focused = !applicationPaused && (preview ? applicationFocused : OVRManager.hasInputFocus);
            if (gate.Current == LaunchGate.Phase.WaitingForRoom)
            {
                waitingTime += Time.unscaledDeltaTime;
                if (waitingTime > 10f && !reportedTimeout)
                {
                    reportedTimeout = true;
                    Debug.LogWarning("Switchyard Raiders opening paused: " + WaitingReason, this);
                }
                bool ready = roomReady && (preview || (OVRManager.isHmdPresent && OVRManager.tracker != null && OVRManager.tracker.isPositionTracked));
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

        public string WaitingReason
        {
            get
            {
                if (preview) return "Desktop preview is waiting for the Game view to have focus.";
                if (!XRSettings.isDeviceActive)
                    return "No active OpenXR headset session. Stop Play, enter Quest Link in the headset, then press Play again. For a flat preview choose Switchyard Raiders > Opening > Use desktop preview (no headset).";
                if (OVRManager.HasInsightPassthroughInitFailed())
                    return "Meta passthrough initialization failed. Check Developer Runtime Features and Passthrough over Meta Quest Link in the PC Link app. Restart Unity if those switches were just enabled.";
                if (!OVRManager.hasInputFocus)
                    return "The headset app does not have input focus. Wear the headset and close the Link/system dashboard.";
                if (!roomReady)
                    return "The OpenXR session is active but Meta has not presented a passthrough frame. Check Passthrough over Meta Quest Link, then restart the Link session and Play.";
                return "Waiting for headset positional tracking. Wear the headset and let tracking recover.";
            }
        }

#if UNITY_EDITOR
        // An editor-only diagnostic, so failed Link initialization never looks like a frozen game.
        private void OnGUI()
        {
            if (preview || gate.Current != LaunchGate.Phase.WaitingForRoom || waitingTime < 3f) return;
            statusStyle ??= new GUIStyle(GUI.skin.box) { wordWrap = true, fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 16, 12, 12) };
            GUI.Box(new Rect(16, 16, Mathf.Max(220f, Mathf.Min(620f, Screen.width - 32f)), 145f),
                "QUEST LINK STARTUP\n\n" + WaitingReason, statusStyle);
        }
#endif

        private void BeginTitle()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // Query once after XR is ready. Do not poll allocating refresh-rate arrays per frame.
            var rates = OVRManager.display.displayFrequenciesAvailable;
            for (int i = 0; rates != null && i < rates.Length; i++)
            {
                if (Mathf.Abs(rates[i] - preferredRefreshRate) > 0.1f) continue;
                OVRManager.display.displayFrequency = rates[i];
                break;
            }
#endif
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
            // Dispose Timeline's playable/audio graph before applying the final static pose.
            director.Stop();
            presentation.HoldFinalPose();
            if (!preview) { PlayerPrefs.SetInt(SeenKey, 1); PlayerPrefs.Save(); }
            enabled = false;
            onTitleReady.Invoke();
        }
    }
}
