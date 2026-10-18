using UnityEngine;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    // Two short translucent curls, only rebuilt during the occasional pressure release.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PressureSteamGraphic : MaskableGraphic
    {
        private float progress = -1;
        protected override void OnEnable()
        {
            if (!GetComponent<CanvasRenderer>()) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }
        public void SetProgress(float value)
        {
            if (Mathf.Abs(progress - value) < .001f) return;
            progress = value; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (progress < 0 || progress >= 1) return;
            const int count = 10;
            for (int strand = 0; strand < 2; strand++)
            {
                int start = vh.currentVertCount;
                for (int i = 0; i <= count; i++)
                {
                    float t = i / (float)count;
                    float x = Mathf.Sin(t * 5 + progress * 3 + strand) * (2 + t * 4) + strand * 5;
                    float y = (t * 25 + progress * 10);
                    float width = .7f + t;
                    Color c = color; c.a *= Mathf.Sin(t * Mathf.PI) * Mathf.Sin(progress * Mathf.PI) * .45f;
                    vh.AddVert(new Vector3(x - width, y, 0), new Color(c.r,c.g,c.b,0), Vector2.zero);
                    vh.AddVert(new Vector3(x, y, 0), c, Vector2.zero);
                    vh.AddVert(new Vector3(x + width, y, 0), new Color(c.r,c.g,c.b,0), Vector2.zero);
                }
                for (int i = 0; i < count; i++) for (int j = 0; j < 2; j++)
                {
                    int a = start + i * 3 + j;
                    vh.AddTriangle(a,a+3,a+4); vh.AddTriangle(a,a+4,a+1);
                }
            }
        }
    }
}
