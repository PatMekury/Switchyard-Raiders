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
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int SweepId = Shader.PropertyToID("_Sweep");

        private void LateUpdate() => Apply();
        public void Apply()
        {
            if (titleRenderer == null || titleVisual == null) return;
            properties ??= new MaterialPropertyBlock();
            titleRenderer.GetPropertyBlock(properties);
            properties.SetFloat(OpacityId, opacity);
            properties.SetFloat(SweepId, sweep);
            titleRenderer.SetPropertyBlock(properties);
            titleVisual.localScale = new Vector3(size.x * settle, size.y * settle, 1f);
            titleVisual.localPosition = new Vector3(0f, verticalOffset, 0f);
        }
    }
}
