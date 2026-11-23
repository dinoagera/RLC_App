using System;
using RLCLab.Core;
using TMPro;
using UnityEngine;

namespace RLCLab
{
    public sealed class CircuitSchematic : LineGraphic
    {
        public CircuitController Circuit;
        public SimulationManager Simulation;
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[5];
        private Vector2 lastSize;
        private bool dirty = true;
        public void Invalidate() => dirty = true;
        private void LateUpdate()
        {
            if (Circuit == null) return;
            if (dirty || lastSize != rectTransform.rect.size || Simulation.IsRunning)
            {
                lastSize = rectTransform.rect.size; dirty = false; Draw();
            }
        }
        private void Caption(int i, string s, float x, float y, float width, Color color)
        {
            if (labels[i] == null) labels[i] = LabTheme.Label(transform, "", 21, color);
            labels[i].text = s;
            LabTheme.Place(labels[i].rectTransform, x, y, width, 30);
        }
        private void Draw()
        {
            strokes.Clear();
            var rect = rectTransform.rect; var p = Circuit.Parameters;
            float w = rect.width, h = rect.height;
            Vector2 P(float x, float y) => new Vector2(x, y) + rect.min;
            void L(float x1, float y1, float x2, float y2, Color? c = null) =>
                Line(P(x1, y1), P(x2, y2), c ?? LabTheme.Muted, 2.6f);
            float left = 100, right = w - 140, bottom = 35, top = h - 35, middle = (top + bottom) / 2;
            float rx = w * .32f, lx = w * .59f;
            L(left, top, rx - 55, top); L(rx + 55, top, lx - 60, top); L(lx + 60, top, right, top);
            L(right, top, right, middle + 10); L(right, middle - 10, right, bottom);
            L(right, bottom, left, bottom); L(left, bottom, left, middle - 27); L(left, middle + 27, left, top);
            Circle(P(left, middle), 27, LabTheme.Voltage);
            Vector2 old = P(left - 17, middle);
            for (int i = 1; i <= 32; i++)
            {
                Vector2 next = P(left - 17 + 34f * i / 32, middle + Mathf.Sin(i * Mathf.PI * 2 / 32) * 9);
                Line(old, next, LabTheme.Voltage); old = next;
            }
            // IEC rectangular resistor.
            L(rx - 55, top - 15, rx + 55, top - 15);
            L(rx - 55, top + 15, rx + 55, top + 15);
            L(rx - 55, top - 15, rx - 55, top + 15);
            L(rx + 55, top - 15, rx + 55, top + 15);
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < 18; i++)
                {
                    float a = Mathf.PI - i * Mathf.PI / 18, b = Mathf.PI - (i + 1) * Mathf.PI / 18;
                    float cx = lx - 45 + j * 30;
                    L(cx + Mathf.Cos(a) * 15, top + Mathf.Sin(a) * 20,
                        cx + Mathf.Cos(b) * 15, top + Mathf.Sin(b) * 20, LabTheme.Blue);
                }
            L(right - 25, middle + 10, right + 25, middle + 10, LabTheme.Violet);
            L(right - 25, middle - 10, right + 25, middle - 10, LabTheme.Violet);
            double instantaneous = Math.Sin(2 * Math.PI * Simulation.PhaseCycles - Circuit.Result.Phase);
            Color flow = LabTheme.Current; flow.a = .2f + .7f * (float)Math.Abs(instantaneous);
            float direction = instantaneous >= 0 ? 1 : -1;
            Arrow(P(w * .49f - 32 * direction, bottom), P(w * .49f + 32 * direction, bottom), flow, 3);
            string F(double n) => n.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            Caption(0, "R · " + F(p.Resistance) + " Ом", rx - 65, h - top + 22, 230, LabTheme.Text);
            Caption(1, "L · " + F(p.Inductance) + " мГн", lx - 65, h - top + 22, 240, LabTheme.Blue);
            Caption(2, "C · " + F(p.Capacitance) + " мкФ", right - 118, h - bottom + 3, 230, LabTheme.Violet);
            Caption(3, F(p.Amplitude) + " В", 8, h - middle - 18, 85, LabTheme.Voltage);
            Caption(4, "f = " + F(p.Frequency) + " Гц", left - 48, h - bottom + 3, 260, LabTheme.Muted);
            SetVerticesDirty();
        }
    }
}
