using UnityEngine;
using UnityEngine.UI;

namespace SwitchyardRaiders.Startup
{
    // A real extruded UI mesh: back wall, bevel and front. Shares Canvas fading and XR material.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MechanicalButtonBody : MaskableGraphic
    {
        public float thickness = 9;
        public float radius = 10;
        public bool fillFront = true;
        public Color edge = new(.08f, .12f, .11f, 1);
        protected override void OnEnable()
        {
            // Also repairs components serialized before CanvasRenderer was required.
            if (!GetComponent<CanvasRenderer>()) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            const int count = 24;
            float corner = Mathf.Min(radius, r.height * .45f);
            for (int ring = 0; ring < 3; ring++)
            {
                float inset = ring == 2 ? 2 : 0;
                float z = ring == 0 ? thickness : ring == 1 ? 2 : 0;
                for (int i = 0; i < count; i++)
                {
                    int q = i / 6;
                    float angle = (q * 90 + (i % 6) * 18) * Mathf.Deg2Rad;
                    var c = new Vector2(q == 0 || q == 3 ? r.xMax - corner : r.xMin + corner,
                        q < 2 ? r.yMax - corner : r.yMin + corner);
                    Vector2 p = c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (corner - inset);
                    var shade = ring == 0 ? edge : ring == 1 ? Color.Lerp(edge, color, .45f) : color;
                    shade.a *= color.a;
                    vh.AddVert(new Vector3(p.x, p.y, z), shade, Vector2.zero);
                }
            }
            for (int ring = 0; ring < 2; ring++)
                for (int i = 0; i < count; i++)
                {
                    int a = ring * count + i, b = ring * count + (i + 1) % count;
                    vh.AddTriangle(a, b, b + count); vh.AddTriangle(a, b + count, a + count);
                }
            if (fillFront)
            {
                vh.AddVert(new Vector3(r.center.x, r.center.y, 0), color, Vector2.zero);
                for (int i = 0; i < count; i++) vh.AddTriangle(count * 3, count * 2 + i, count * 2 + (i + 1) % count);
            }
        }
    }
}
