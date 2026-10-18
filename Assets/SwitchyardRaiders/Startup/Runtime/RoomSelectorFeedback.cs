using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    // Only visual children move; the SDK's canvas and button hit rectangle remain fixed.
    public sealed class RoomSelectorFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform face;
        public UnityEngine.UI.Image artwork;
        public Text detail;
        public RoomChoiceMenu menu;
        public bool virtualChoice;
        public float arrivalDelay;
        public RectTransform illustration;
        public UnityEngine.UI.Image signal;
        [Tooltip("Replace this Image's Source Image with your final icon.")]
        public UnityEngine.UI.Image replaceableIcon;
        public RectTransform pressureNeedle;
        public PressureSteamGraphic steam;
        private readonly HashSet<int> hovering = new(), pressing = new();
        private UnityEngine.UI.Button button;
        private Vector3 rest, illustrationRest;
        private Quaternion illustrationRotation;
        private float depth, hoverAmount;
        private float clock, confirmTime;

        private void Awake()
        {
            rest = face.localPosition;
            button = GetComponent<UnityEngine.UI.Button>();
            if (illustration) { illustrationRest = illustration.localPosition; illustrationRotation = illustration.localRotation; }
            clock = virtualChoice ? 2.7f : 0;
        }
        private void OnDisable()
        {
            hovering.Clear(); pressing.Clear();
            if (menu && menu.follower) menu.follower.HoldFor(this, false);
            if (face) face.localPosition = rest;
            if (illustration) { illustration.localPosition = illustrationRest; illustration.localRotation = illustrationRotation; }
            depth = hoverAmount = 0;
            confirmTime = 0;
            if (steam) steam.SetProgress(-1);
        }
        public void Confirm() => confirmTime = .3f;
        private void Update()
        {
            if (!button.IsInteractable())
            {
                pressing.Clear();
                if (menu.follower) menu.follower.HoldFor(this, false);
            }
            bool hover = button.IsInteractable() && hovering.Count > 0;
            clock += Time.unscaledDeltaTime;
            confirmTime = Mathf.Max(0, confirmTime - Time.unscaledDeltaTime);
            float target = pressing.Count > 0 || confirmTime > 0 ? 3f : 0f;
            float rate = 1 - Mathf.Exp(-22 * Time.unscaledDeltaTime);
            depth = Mathf.Lerp(depth, target, rate);
            if (Mathf.Abs(depth - target) < .001f) depth = target;
            Vector3 position = rest + new Vector3(0, 0, depth);
            if (face.localPosition != position) face.localPosition = position;
            hoverAmount = Mathf.Lerp(hoverAmount, hover ? 1 : 0, rate);
            if (Mathf.Abs(hoverAmount - (hover ? 1 : 0)) < .001f) hoverAmount = hover ? 1 : 0;
            if (illustration)
            {
                illustration.localPosition = illustrationRest + new Vector3(0, hoverAmount * 3, -hoverAmount * 1.5f);
                illustration.localRotation = illustrationRotation * Quaternion.Euler(0, 0, hoverAmount * (virtualChoice ? -2 : 2));
            }
            if (pressureNeedle)
                pressureNeedle.localRotation = Quaternion.Euler(0, 0, -28 + Mathf.Sin(clock * 1.2f) * 7 - hoverAmount * 28);
            if (signal)
            {
                var color = confirmTime > 0 ? new Color(.54f, 1f, .58f) : Color.Lerp(new Color(.35f, .43f, .35f), new Color(1f, .8f, .36f), hoverAmount);
                if (signal.color != color) signal.color = color;
            }
        }
        public void OnPointerEnter(PointerEventData e) => hovering.Add(e.pointerId);
        public void OnPointerExit(PointerEventData e)
        {
            hovering.Remove(e.pointerId); pressing.Remove(e.pointerId);
            if (menu.follower) menu.follower.HoldFor(this, pressing.Count > 0);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (!button.IsInteractable()) return;
            pressing.Add(e.pointerId);
            if (menu.follower) menu.follower.HoldFor(this, true);
        }
        public void OnPointerUp(PointerEventData e)
        {
            pressing.Remove(e.pointerId);
            if (menu.follower) menu.follower.HoldFor(this, pressing.Count > 0);
        }
    }
}

