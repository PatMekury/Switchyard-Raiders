using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    public sealed class StationMainMenu : MonoBehaviour
    {
        public RoomChoiceMenu roomMenu;
        public GameObject content;
        public Text status;
        public Button matchButton, friendsButton, practiceButton;
        public RaidersMenu navigation;
        public UnityEvent onFindMatch = new(), onPlayWithFriends = new(), onPractice = new();
        public bool IsOpen => content && content.activeSelf;
        private void Awake() { if (content) content.SetActive(false); }
        public void Show()
        {
            if (!IsOpen && roomMenu.menuAudio) roomMenu.menuAudio.MenuOpened();
            roomMenu.selectionRoot.SetActive(false);
            roomMenu.panelRoot.SetActive(true);
            roomMenu.group.alpha = 1;
            roomMenu.group.interactable = roomMenu.group.blocksRaycasts = true;
            content.SetActive(true);
            // Gameplay routes must be connected before advertising them as playable.
            matchButton.interactable = navigation || onFindMatch.GetPersistentEventCount() > 0;
            friendsButton.interactable = navigation || onPlayWithFriends.GetPersistentEventCount() > 0;
            practiceButton.interactable = navigation || onPractice.GetPersistentEventCount() > 0;
            status.text = matchButton.interactable || friendsButton.interactable || practiceButton.interactable
                ? "" : "Game modes are not connected yet";
        }
        public void Hide() => content.SetActive(false);
        public void ChangeRoom() { Hide(); roomMenu.Show(); }
        public void FindMatch() { if (matchButton.IsInteractable()) { if(navigation)navigation.OpenPublic();else onFindMatch.Invoke(); } }
        public void PlayWithFriends() { if (friendsButton.IsInteractable()) { if(navigation)navigation.OpenFriends();else onPlayWithFriends.Invoke(); } }
        public void Practice() { if (practiceButton.IsInteractable()) { if(navigation)navigation.OpenPractice();else onPractice.Invoke(); } }
        public void Settings() { if(navigation)navigation.OpenSettings(); }
    }
}
