using System;
using RLCLab.Core;
using TMPro;
using UnityEngine;

namespace RLCLab
{
    public sealed class PhasorDiagram : LineGraphic
    {
        public CircuitController Circuit;
        private bool dirty = true;
        private Vector2 lastSize;
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[9];
        public void Invalidate() => dirty = true;
        private void LateUpdate()
        {
            if (Circuit == null) return;
            if (dirty || lastSize != rectTransform.rect.size)
            {
                lastSize = rectTransform.rect.size; dirty = false; Draw();
            }
        }
        private void Caption(int index, string text, Vector2 point, Color color, float width = 250)
        {
            if (labels[index] == null) labels[index] = LabTheme.Label(transform, "", 19, color);
            labels[index].text = text; labels[index].color = color;
            var r = rectTransform.rect;
            LabTheme.Place(labels[index].rectTransform, Mathf.Clamp(point.x - r.xMin, 8, r.width - width),
                Mathf.Clamp(r.yMax - point.y - 12, 5, r.height - 30), width, 28);
        }
        private static string F(double n) => n.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        private void Draw()
        {
            strokes.Clear();
            var r = Circuit.Result;
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.height * .34f, rect.width * .25f);
            Vector2 o = new Vector2(rect.center.x - rect.width * .09f, rect.center.y + 5);
            Circle(o, radius, LabTheme.Border);
            Circle(o, radius / 2, LabTheme.Border);
            Line(o - Vector2.right * (radius + 24), o + Vector2.right * (radius + 35), LabTheme.Border);
            Line(o - Vector2.up * (radius + 20), o + Vector2.up * (radius + 20), LabTheme.Border);
            double voltageScale = Math.Max(Circuit.Parameters.Amplitude, Math.Max(r.UR, Math.Max(r.UL, r.UC)));
            float k = radius / (float)voltageScale;
            Vector2 ur = o + Vector2.right * (float)r.UR * k;
            Vector2 ul = o + Vector2.up * (float)r.UL * k;
            Vector2 uc = o - Vector2.up * (float)r.UC * k;
            Vector2 u = o + new Vector2((float)r.UR, (float)(r.UL - r.UC)) * k;
            Arrow(o, ul, LabTheme.Blue);
            Arrow(o, uc, LabTheme.Violet);
            Arrow(o, ur, LabTheme.Muted, 5);
            Arrow(o, u, LabTheme.Voltage, 3);
            // Current has a separate scale and a separate lane to avoid hiding UR at phi=0.
            Vector2 io = new Vector2(o.x - radius, rect.yMin + 43);
            Arrow(io, io + Vector2.right * radius * 1.4f, LabTheme.Current, 3);
            Vector2 old = o + Vector2.right * radius * .32f;
            for (int i = 1; i <= 48; i++)
            {
                float a = (float)r.Phase * i / 48;
                Vector2 next = o + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius * .32f;
                Line(old, next, LabTheme.Voltage); old = next;
            }
            Caption(0, "Uₘ = " + F(Circuit.Parameters.Amplitude) + " В", u + new Vector2(12, 25), LabTheme.Voltage);
            Caption(1, "Uᵣ = " + F(r.UR) + " В", ur + new Vector2(12, -28), LabTheme.Muted);
            Caption(2, "Uₗ = " + F(r.UL) + " В", ul + new Vector2(12, 15), LabTheme.Blue);
            Caption(3, "U<sub>C</sub> = " + F(r.UC) + " В", uc + new Vector2(12, -20), LabTheme.Violet);
            Caption(4, "Iₘ = " + F(r.Im) + " А → опорный вектор", io + new Vector2(0, -25), LabTheme.Current, 420);
            Caption(5, "φ = " + F(r.PhaseDegrees) + "°", o + new Vector2(radius * .4f, 45), LabTheme.Voltage);
            Caption(6, "Re", o + new Vector2(radius + 40, 0), LabTheme.Muted, 70);
            Caption(7, "Im", o + new Vector2(-40, radius + 26), LabTheme.Muted, 70);
            Caption(8, "U = Uᵣ + Uₗ + U<sub>C</sub> · I — отдельный масштаб",
                new Vector2(rect.xMin + 15, rect.yMax - 15), LabTheme.Muted, 680);
            SetVerticesDirty();
        }
    }
}
