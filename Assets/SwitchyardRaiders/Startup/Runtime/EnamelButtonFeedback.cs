using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    public sealed class EnamelButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform face;
        public UnityEngine.UI.Image image;
        public SeatedPanelFollow follower;
        private readonly HashSet<int> hovering = new(), pressing = new();
        private Material material;
        private float glow, depth;
        private Vector3 restPosition;
        private static readonly int Hover = Shader.PropertyToID("_Hover");
        private static readonly int PressTime = Shader.PropertyToID("_PressTime");
        private void Awake()
        {
            restPosition = face.localPosition;
            material = new Material(image.material);
            image.material = material;
        }
        private void OnDestroy() { if (material) Destroy(material); }
        private void OnDisable()
        {
            hovering.Clear(); pressing.Clear();
            if (face) face.localPosition = restPosition;
            if (follower) follower.HoldFor(this, false);
            glow = depth = 0;
            if (material) material.SetFloat(Hover, 0);
        }
        private void Update()
        {
            bool down = pressing.Count > 0;
            float targetGlow = hovering.Count > 0 ? 1 : 0;
            float targetDepth = down ? 3 : hovering.Count > 0 ? -2 : 0;
            if (Mathf.Abs(glow-targetGlow) < .0001f && Mathf.Abs(depth-targetDepth) < .0001f) return;
            float t = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            glow = Mathf.Lerp(glow, targetGlow, t);
            depth = Mathf.Lerp(depth, targetDepth, t);
            // Only artwork moves; the touch surface and hit target remain fixed.
            face.localPosition = restPosition + new Vector3(0, 0, depth);
            material.SetFloat(Hover, glow);
        }
        public void OnPointerEnter(PointerEventData e) { hovering.Add(e.pointerId); }
        public void OnPointerExit(PointerEventData e) { hovering.Remove(e.pointerId); pressing.Remove(e.pointerId); follower.HoldFor(this, pressing.Count > 0); }
        public void OnPointerDown(PointerEventData e)
        {
            if (!GetComponent<UnityEngine.UI.Button>().IsInteractable()) return;
            pressing.Add(e.pointerId); follower.HoldFor(this, true);
            material.SetFloat(PressTime, Time.time);
        }
        public void OnPointerUp(PointerEventData e) { pressing.Remove(e.pointerId); follower.HoldFor(this, pressing.Count > 0); }
    }
}
