using UnityEngine;

namespace SwitchyardRaiders.Startup
{
    /// <summary>Timeline animates these values; one unlit draw keeps the room unobscured.</summary>
    [ExecuteAlways]
    public sealed class LaunchPresentation : MonoBehaviour
    {
        [Range(0f, 1f)] public float opacity;
        [Range(-1f, 2f)] public float sweep = -1f;
        public float settle = 0.965f;
        public float verticalOffset = -0.025f;
        public Renderer titleRenderer;
        public Transform titleVisual;
        public Vector2 size = new(0.92f, 0.6982f);
        private MaterialPropertyBlock properties;
        private Animator titleAnimator;
        private Vector4 appliedValues;
        private Vector2 appliedSize;
        private bool hasAppliedValues;
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int SweepId = Shader.PropertyToID("_Sweep");

        private void OnEnable()
        {
            titleAnimator = GetComponent<Animator>();
            hasAppliedValues = false;
        }
        private void LateUpdate() => Apply();
        public void ResetForPlayback()
        {
            enabled = true;
            titleAnimator = GetComponent<Animator>();
            if (titleAnimator != null) titleAnimator.enabled = true;
            opacity = 0f;
            settle = 0.965f;
            verticalOffset = -0.025f;
            sweep = -1f;
            hasAppliedValues = false;
            Apply();
        }

        public void Apply()
        {
            if (titleRenderer == null || titleVisual == null) return;
            var values = new Vector4(opacity, sweep, settle, verticalOffset);
            if (hasAppliedValues && values == appliedValues && size == appliedSize) return;
            properties ??= new MaterialPropertyBlock();
            properties.SetFloat(OpacityId, opacity);
            properties.SetFloat(SweepId, sweep);
            titleRenderer.SetPropertyBlock(properties);
            titleVisual.localScale = new Vector3(size.x * settle, size.y * settle, 1f);
            titleVisual.localPosition = new Vector3(0f, verticalOffset, 0f);
            appliedValues = values;
            appliedSize = size;
            hasAppliedValues = true;
        }

        public void HoldFinalPose()
        {
            if (titleAnimator != null) titleAnimator.enabled = false;
            opacity = 1f;
            settle = 1f;
            verticalOffset = 0f;
            sweep = 2f;
            Apply();
            // The settled nameplate needs no animation or material uploads every frame.
            enabled = false;
        }
    }
}
