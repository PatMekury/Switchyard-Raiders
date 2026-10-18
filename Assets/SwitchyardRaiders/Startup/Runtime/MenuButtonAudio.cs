using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    [RequireComponent(typeof(Button))]
    public sealed class MenuButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public MenuInteractionAudio audioBus;
        public AudioClip clickClip;
        private Button button;
        private readonly HashSet<int> hovering = new();
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(PlayClick);
        }
        private void OnDestroy()
        {
            if (button) button.onClick.RemoveListener(PlayClick);
        }
        private void OnDisable() => hovering.Clear();
        public void OnPointerEnter(PointerEventData data)
        {
            if (!button || !button.IsActive() || !button.IsInteractable()) return;
            bool first = hovering.Count == 0;
            if (hovering.Add(data.pointerId) && first && audioBus) audioBus.Hover();
        }
        public void OnPointerExit(PointerEventData data) => hovering.Remove(data.pointerId);
        private void PlayClick()
        {
            // Button already validated this click. Its navigation callback may now have
            // disabled the panel, so do not re-check IsInteractable after navigation.
            if (audioBus) audioBus.Click(clickClip);
        }
    }
}
