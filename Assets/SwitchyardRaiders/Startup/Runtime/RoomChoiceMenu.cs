using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    public sealed class RoomChoiceMenu : MonoBehaviour
    {
        public LaunchSequence launch;
        public SeatedPanelFollow follower;
        public Canvas canvas;
        public CanvasGroup group;
        public GameObject panelRoot;
        public GameObject virtualRoom;
        public Text heading, status;
        public UnityEngine.UI.Button realButton, virtualButton;
        public UnityEvent onRealRoomSelected = new();
        public UnityEvent onVirtualRoomSelected = new();
        public bool IsOpen { get; private set; }
        public bool IsVirtual { get; private set; }
        public bool HasSelection { get; private set; }
        private bool busy;
        private bool roomPositioned;
        private Coroutine transition;

        private void Awake()
        {
            panelRoot.SetActive(false);
            virtualRoom.SetActive(false);
            group.alpha = 0;
            group.interactable = group.blocksRaycasts = false;
            launch.onTitleReady.AddListener(Show);
        }

        private void OnDestroy() => launch.onTitleReady.RemoveListener(Show);

        public void Show()
        {
            if (IsOpen) return;
            IsOpen = true;
            canvas.worldCamera = launch.headset.GetComponent<Camera>();
            follower.Snap(launch.headset);
            panelRoot.SetActive(true);
            transition = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            // Crossfade the existing Timeline title into the first interactive screen.
            float elapsed = 0;
            while (elapsed < .4f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / .4f);
                group.alpha = t;
                launch.presentation.opacity = 1 - t;
                launch.presentation.Apply();
                yield return null;
            }
            launch.presentation.titleRenderer.enabled = false;
            if (launch.titleFollow) launch.titleFollow.enabled = false;
            group.alpha = 1;
            group.interactable = group.blocksRaycasts = true;
            status.text = launch.IsDesktopPreview || launch.HasRoomFrame ? "Touch a destination, or point and pinch" : "Room video unavailable. Choose Virtual Room.";
            transition = null;
        }

        public void ChooseRealRoom() => Choose(false);
        public void ChooseVirtualRoom() => Choose(true);

        private void Choose(bool useVirtual)
        {
            if (!IsOpen || busy || group.alpha < .99f) return;
            if (!useVirtual && !launch.IsDesktopPreview && !launch.HasRoomFrame)
            {
                status.text = "Room video unavailable. Choose Virtual Room.";
                return;
            }
            if (HasSelection && IsVirtual == useVirtual) return;
            transition = StartCoroutine(ChangeRoom(useVirtual));
        }

        private IEnumerator ChangeRoom(bool useVirtual)
        {
            busy = true;
            realButton.interactable = virtualButton.interactable = false;
            if (useVirtual && !roomPositioned)
            {
                Vector3 position = launch.headset.position;
                // Floor-level tracking: keep the virtual room on the tracking-space floor.
                position.y = launch.IsDesktopPreview ? 0 : launch.xrRigRoot.GetComponent<OVRCameraRig>().trackingSpace.position.y;
                virtualRoom.transform.position = position;
                virtualRoom.transform.rotation = Quaternion.Euler(0, launch.headset.eulerAngles.y, 0);
                roomPositioned = true;
            }
            // Reveal geometry underneath passthrough before removing the room layer.
            if (useVirtual) virtualRoom.SetActive(true);
            if (launch.passthrough) launch.passthrough.hidden = false;
            float elapsed = 0;
            float start = launch.passthrough ? launch.passthrough.textureOpacity : 1;
            while (elapsed < .3f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / .3f);
                if (launch.passthrough) launch.passthrough.textureOpacity = Mathf.Lerp(start, useVirtual ? 0 : 1, t);
                yield return null;
            }
            if (launch.passthrough) launch.passthrough.hidden = useVirtual;
            virtualRoom.SetActive(useVirtual);
            IsVirtual = useVirtual;
            HasSelection = true;
            heading.text = "CHOOSE YOUR ROOM";
            status.text = useVirtual ? "Virtual room ready / Choose either room to switch" : "Your room ready / Choose either room to switch";
            realButton.interactable = virtualButton.interactable = true;
            busy = false;
            transition = null;
            if (useVirtual) onVirtualRoomSelected.Invoke(); else onRealRoomSelected.Invoke();
        }
    }
}
