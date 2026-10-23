using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    public sealed class RoomChoiceMenu : MonoBehaviour
    {
        public LaunchSequence launch;
        public RaidersRoom roomUnderstanding;
        public RaidersTable table;
        public GameObject cancelModeButton;
        public bool Transitioning => busy || requestingRoom;
        private int roomRequest;
        private bool requestingRoom;
        public SeatedPanelFollow follower;
        public Canvas canvas;
        public CanvasGroup group;
        public GameObject panelRoot;
        public GameObject virtualRoom;
        public Text heading, status;
        public GameObject roomUnavailablePopup;
        public Text roomUnavailableHeading, roomUnavailableMessage;
        public UnityEngine.UI.Button realButton, virtualButton;
        public ModularRoomMenuMotion motion;
        public GameObject selectionRoot;
        public StationMainMenu mainMenu;
        public MenuInteractionAudio menuAudio;
        public UnityEvent onRealRoomSelected = new();
        public UnityEvent onVirtualRoomSelected = new();
        public bool IsOpen { get; private set; }
        public bool IsVirtual { get; private set; }
        public bool HasSelection { get; private set; }
        private bool busy;
        private bool roomPositioned;
        private Coroutine transition;
        private Vector3 menuScale;
        private float menuHeight;
        private bool gameplayView;

        private void Awake()
        {
            menuScale = canvas.transform.localScale;
            menuHeight = follower.heightOffset;
            panelRoot.SetActive(false);
            virtualRoom.SetActive(false);
            if (roomUnavailablePopup) roomUnavailablePopup.SetActive(false);
            group.alpha = 0;
            group.interactable = group.blocksRaycasts = false;
            launch.onTitleReady.AddListener(Show);
        }

        private void OnDestroy() => launch.onTitleReady.RemoveListener(Show);

        private IEnumerator Start()
        {
            if (launch.IsDesktopPreview) yield break;
            // Wait for tracked startup; this check never prompts or blocks the VR option.
            while (launch.Phase == LaunchGate.Phase.WaitingForRoom) yield return null;
            if (roomUnderstanding) roomUnderstanding.CheckOnStartup(launch.headset);
        }

        public void Show()
        {
            if (IsOpen) return;
            SetGameplayView(false);
            if(table)table.PrepareModeChange();
            if(cancelModeButton)cancelModeButton.SetActive(HasSelection);
            if (menuAudio) menuAudio.MenuOpened();
            if (mainMenu) mainMenu.Hide();
            if (selectionRoot) selectionRoot.SetActive(true);
            if (roomUnavailablePopup) roomUnavailablePopup.SetActive(false);
            realButton.interactable = virtualButton.interactable = true;
            IsOpen = true;
            canvas.worldCamera = launch.headset.GetComponent<Camera>();
            follower.Snap(launch.headset);
            panelRoot.SetActive(true);
            if (motion) motion.Reveal();
            transition = StartCoroutine(Reveal());
        }

        private IEnumerator Reveal()
        {
            // Crossfade the existing Timeline title into the first interactive screen.
            float elapsed = 0;
            float duration = motion ? .65f : .4f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                group.alpha = t;
                launch.presentation.opacity = 1 - t;
                launch.presentation.Apply();
                yield return null;
            }
            launch.presentation.titleRenderer.enabled = false;
            if (launch.titleFollow) launch.titleFollow.enabled = false;
            group.alpha = 1;
            group.interactable = group.blocksRaycasts = true;
            status.text = "Play in VR anywhere, or use your saved room.";
            transition = null;
        }

        public async void ChooseRealRoom()
        {
            if (!IsOpen || busy || requestingRoom || group.alpha < .99f) return;
            if(roomUnderstanding && roomUnderstanding.HasPreview)
            {
                if(roomUnderstanding.Confirm(launch.headset))Choose(false);
                else {if(table)table.CancelPreview();ShowRoomUnavailable("Placement changed",roomUnderstanding.Status);}
                return;
            }
            int request = ++roomRequest; requestingRoom = true; realButton.interactable = false;
            try
            {
                if (!roomUnderstanding) { ShowRoomUnavailable("Room unavailable", "Play in VR instead."); return; }
                bool loaded = await roomUnderstanding.Load(launch.headset);
                if (!this || request != roomRequest || !IsOpen) return;
                status.text = roomUnderstanding.Status;
                if (loaded && roomUnderstanding.TryPlace(launch.headset, out var pose))
                {if(table)table.Preview(pose,roomUnderstanding.PreviewScale);status.text=roomUnderstanding.Status;}
                else ShowRoomUnavailable(roomUnderstanding.Availability == RaidersRoom.RoomAvailability.NotFound ? "Room not found" : "Room unavailable", roomUnderstanding.Status);
            }
            finally
            {
                if (this && request == roomRequest) { requestingRoom = false; realButton.interactable = !busy; }
            }
        }
        public void SetGameplayView(bool gameplay)
        {
            if (gameplayView == gameplay) return;
            gameplayView = gameplay;
            // Keep the controls above the yard instead of occluding the playfield.
            canvas.transform.localScale = menuScale * (gameplay ? .76f : 1f);
            follower.heightOffset = gameplay ? .11f : menuHeight;
            follower.Snap(launch.headset);
        }
        private void ShowRoomUnavailable(string title, string explanation)
        {
            status.text = explanation;
            if (!roomUnavailablePopup) return;
            roomUnavailableHeading.text = title;
            roomUnavailableMessage.text = explanation;
            selectionRoot.SetActive(false);
            roomUnavailablePopup.SetActive(true);
        }
        public void DismissRoomUnavailable()
        {
            if (roomUnavailablePopup) roomUnavailablePopup.SetActive(false);
            if (IsOpen) selectionRoot.SetActive(true);
        }
        private void Update()
        {
            if (requestingRoom && IsOpen && roomUnderstanding) status.text = roomUnderstanding.Status;
        }
        public void ChooseVirtualRoom()
        {
            if (!IsOpen || busy || group.alpha < .99f) return;
            ++roomRequest; requestingRoom = false;
            if (roomUnderstanding) {roomUnderstanding.CancelSelection();roomUnderstanding.CancelPreview();}
            if(table)table.CancelPreview();
            DismissRoomUnavailable();
            Choose(true);
        }

        private void Choose(bool useVirtual)
        {
            if (!IsOpen || busy || group.alpha < .99f) return;
            if (!useVirtual && !launch.IsDesktopPreview && !launch.HasRoomFrame)
            {
                ShowRoomUnavailable("Room video unavailable", "Play in VR instead.");
                return;
            }
            transition = StartCoroutine(ChangeRoom(useVirtual));
        }

        private IEnumerator ChangeRoom(bool useVirtual)
        {
            busy = true;
            realButton.interactable = virtualButton.interactable = false;
            group.interactable = group.blocksRaycasts = false;
            var feedback = (useVirtual ? virtualButton : realButton).GetComponent<RoomSelectorFeedback>();
            if (feedback) feedback.Confirm();
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
            yield return HidePieces();
            busy = false;
            transition = null;
            if(table)table.ModeChosen();
            if(mainMenu && mainMenu.navigation && mainMenu.navigation.session.Phase!=RaidersSessionPhase.Idle)mainMenu.navigation.ResumeSessionView();
            else if(mainMenu)mainMenu.Show();
            if (useVirtual) onVirtualRoomSelected.Invoke(); else onRealRoomSelected.Invoke();
        }

        public void AbortPendingSelection()
        {
            if(!IsOpen && !busy && !requestingRoom)return;
            ++roomRequest;requestingRoom=false;
            if(transition!=null)StopCoroutine(transition);transition=null;busy=false;IsOpen=false;
            if(roomUnderstanding){roomUnderstanding.CancelSelection();roomUnderstanding.CancelPreview();}
            if(table)table.CancelPreview();
            if(roomUnavailablePopup)roomUnavailablePopup.SetActive(false);
            if(cancelModeButton)cancelModeButton.SetActive(false);
            selectionRoot.SetActive(false);virtualRoom.SetActive(HasSelection && IsVirtual);
            if(launch.passthrough){launch.passthrough.hidden=HasSelection && IsVirtual;launch.passthrough.textureOpacity=HasSelection && IsVirtual?0:1;}
            group.alpha=1;group.interactable=group.blocksRaycasts=true;
        }
        public void CancelModeChange()
        {
            if(!HasSelection || busy)return;
            ++roomRequest;requestingRoom=false;
            if(roomUnderstanding){roomUnderstanding.CancelSelection();roomUnderstanding.CancelPreview();}
            if(table)table.CancelPreview();
            IsOpen=false;panelRoot.SetActive(false);
            if(mainMenu.navigation.session.Phase!=RaidersSessionPhase.Idle)mainMenu.navigation.ResumeSessionView();else mainMenu.Show();
        }
        public void Hide()
        {
            if (!IsOpen || busy) return;
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(HidePieces());
        }

        private IEnumerator HidePieces()
        {
            group.interactable = group.blocksRaycasts = false;
            if (motion)
            {
                motion.Exit();
                while (!motion.ExitComplete) yield return null;
            }
            group.alpha = 0;
            panelRoot.SetActive(false);
            IsOpen = false;
            transition = null;
        }
    }
}
