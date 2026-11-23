using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RLCLab
{
    /// <summary>Batched anti-alias-free UI mesh; no GameObjects per sample or frame.</summary>
    public class LineGraphic : MaskableGraphic
    {
        protected readonly List<Stroke> strokes = new List<Stroke>();
        protected struct Stroke { public Vector2 A, B; public Color Color; public float Width; }
        protected void Line(Vector2 a, Vector2 b, Color c, float width = 2)
            => strokes.Add(new Stroke { A = a, B = b, Color = c, Width = width });
        protected void Arrow(Vector2 a, Vector2 b, Color c, float width = 3)
        {
            Line(a, b, c, width);
            if ((b - a).sqrMagnitude < 1) return;
            Vector2 d = (a - b).normalized, n = new Vector2(-d.y, d.x);
            Line(b, b + d * 12 + n * 6, c, width);
            Line(b, b + d * 12 - n * 6, c, width);
        }
        protected void Circle(Vector2 center, float radius, Color c, int segments = 64)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
                Line(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                    center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, c);
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach (var s in strokes)
            {
                Vector2 delta = s.B - s.A;
                if (delta.sqrMagnitude < .00001f) continue;
                Vector2 n = new Vector2(-delta.y, delta.x).normalized * s.Width / 2;
                int v = vh.currentVertCount;
                vh.AddVert(s.A - n, s.Color, Vector2.zero);
                vh.AddVert(s.A + n, s.Color, Vector2.zero);
                vh.AddVert(s.B + n, s.Color, Vector2.zero);
                vh.AddVert(s.B - n, s.Color, Vector2.zero);
                vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v + 2, v + 3, v);
            }
        }
    }
}
