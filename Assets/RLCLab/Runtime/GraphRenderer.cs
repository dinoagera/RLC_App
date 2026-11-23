using System;
using RLCLab.Core;
using TMPro;
using UnityEngine;

namespace RLCLab
{
    public enum GraphMode { Time, Phasor, Frequency }

    public sealed class GraphRenderer : LineGraphic
    {
        public CircuitController Circuit;
        public SimulationManager Simulation;
        public GraphMode Mode;
        public bool Normalized;
        private readonly TextMeshProUGUI[] labels = new TextMeshProUGUI[42];
        private int labelCount;
        private double[] frequencies;
        private bool dirty = true;
        private Vector2 lastSize;
        private GraphMode lastMode;
        private bool lastNormalized;
        public void Invalidate() { frequencies = null; dirty = true; }

        private void LateUpdate()
        {
            if (Circuit == null || Simulation == null || Mode == GraphMode.Phasor) return;
            Vector2 size = rectTransform.rect.size;
            if (dirty || size != lastSize || lastMode != Mode || lastNormalized != Normalized
                || (Mode == GraphMode.Time && Simulation.IsRunning))
            {
                dirty = false; lastSize = size; lastMode = Mode; lastNormalized = Normalized;
                Draw();
            }
        }
        private Vector2 Point(float x, float y) => new Vector2(x, y) + rectTransform.rect.min;
        private void Segment(float x1, float y1, float x2, float y2, Color color, float width = 1)
            => Line(Point(x1, y1), Point(x2, y2), color, width);
        private void Text(string value, float x, float y, float width, Color color, float size = 17)
        {
            if (labelCount >= labels.Length) return;
            var label = labels[labelCount];
            if (label == null) labels[labelCount] = label = LabTheme.Label(transform, "", size, color);
            label.gameObject.SetActive(true); label.text = value; label.color = color; label.fontSize = size;
            LabTheme.Place(label.rectTransform, x, rectTransform.rect.height - y - 12, width, 25);
            labelCount++;
        }
        private static string Number(double n) => n.ToString(Math.Abs(n) >= 100 ? "0" : "0.###",
            System.Globalization.CultureInfo.InvariantCulture);
        private void Draw()
        {
            strokes.Clear(); labelCount = 0;
            if (Mode == GraphMode.Time) DrawTime(); else DrawFrequency();
            for (int i = labelCount; i < labels.Length; i++) if (labels[i] != null) labels[i].gameObject.SetActive(false);
            SetVerticesDirty();
        }
        private void DrawTime()
        {
            var p = Circuit.Parameters; var r = Circuit.Result;
            float left = 76, right = rectTransform.rect.width - 30;
            float bottom = 54, top = rectTransform.rect.height - 42;
            float w = right - left, h = top - bottom;
            double max = Normalized ? 1.15 : Math.Max(p.Amplitude, r.Im) * 1.15;
            for (int j = 0; j <= 4; j++)
            {
                float y = bottom + h * j / 4;
                Segment(left, y, right, y, LabTheme.Border, j == 2 ? 2 : 1);
                Text(Number(max * (j / 2.0 - 1)), 1, y, 72, LabTheme.Muted);
            }
            for (int j = 0; j <= 6; j++)
            {
                float x = left + w * j / 6;
                Segment(x, bottom, x, top, LabTheme.Border);
                double t = (Simulation.PhaseCycles + j * .5) / p.Frequency * 1000;
                Text(Number(t), x - 20, bottom - 22, 90, LabTheme.Muted);
            }
            Text(Normalized ? "u / Uₘ · i / Iₘ" : "u, В · i, А", left, top + 23, 260, LabTheme.Muted);
            Text("t, мс", right - 60, bottom - 44, 100, LabTheme.Muted);
            Vector2 lastU = default, lastI = default;
            const int samples = 760;
            for (int i = 0; i <= samples; i++)
            {
                double theta = 2 * Math.PI * (Simulation.PhaseCycles + 3.0 * i / samples);
                double u = Math.Sin(theta) * (Normalized ? 1 : p.Amplitude);
                double current = Math.Sin(theta - r.Phase) * (Normalized ? 1 : r.Im);
                var a = Point(left + w * i / samples, bottom + h * (float)(.5 + u / max / 2));
                var b = Point(left + w * i / samples, bottom + h * (float)(.5 + current / max / 2));
                if (i > 0)
                {
                    Line(lastU, a, LabTheme.Voltage, 3);
                    if (i % 10 < 6) Line(lastI, b, LabTheme.Current, 2);
                }
                lastU = a; lastI = b;
            }
        }
        private void DrawFrequency()
        {
            var p = Circuit.Parameters; var r = Circuit.Result;
            if (frequencies == null) frequencies = FrequencySampler.Create(p);
            float left = 90, right = rectTransform.rect.width - 30, width = right - left;
            float low = 54, high = rectTransform.rect.height - 38, gap = 56;
            float h = (high - low - gap) / 2;
            float topBottom = low + h + gap;
            double imax = 0, zmax = 0;
            foreach (double f in frequencies)
            {
                double z = CircuitCalculator.ImpedanceAt(p, f);
                imax = Math.Max(imax, p.Amplitude / z); zmax = Math.Max(zmax, z);
            }
            imax *= 1.12; zmax *= 1.12;
            for (int chart = 0; chart < 2; chart++)
            {
                float b = chart == 0 ? topBottom : low;
                double max = chart == 0 ? imax : zmax;
                for (int j = 0; j <= 2; j++)
                {
                    float y = b + h * j / 2;
                    Segment(left, y, right, y, LabTheme.Border);
                    Text(Number(max * j / 2), 1, y, 85, LabTheme.Muted);
                }
            }
            Text("Iₘ, А", left, high + 20, 200, LabTheme.Current);
            Text("Z, Ом", left, low + h + 20, 200, LabTheme.Violet);
            double[] ticks = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
            foreach (double f in ticks)
            {
                float x = left + (float)(Math.Log10(f) / 3) * width;
                Segment(x, low, x, low + h, LabTheme.Border);
                Segment(x, topBottom, x, high, LabTheme.Border);
                Text(f.ToString("0"), x - 12, low - 22, 65, LabTheme.Muted, 16);
            }
            Text("f, Гц · логарифмическая ось", right - 340, 9, 350, LabTheme.Muted);
            Vector2 lastI = default, lastZ = default;
            for (int i = 0; i < frequencies.Length; i++)
            {
                double f = frequencies[i], z = CircuitCalculator.ImpedanceAt(p, f);
                float x = left + (float)(Math.Log10(f) / 3) * width;
                Vector2 a = Point(x, topBottom + h * (float)(p.Amplitude / z / imax));
                Vector2 b = Point(x, low + h * (float)(z / zmax));
                if (i > 0) { Line(lastI, a, LabTheme.Current, 2.6f); Line(lastZ, b, LabTheme.Violet, 2.6f); }
                lastI = a; lastZ = b;
            }
            if (r.ResonanceFrequency >= 1 && r.ResonanceFrequency <= 1000)
            {
                float x = left + (float)(Math.Log10(r.ResonanceFrequency) / 3) * width;
                for (float y = low; y < high; y += 12)
                    Segment(x, y, x, Mathf.Min(y + 6, high), LabTheme.Voltage, 2);
                Text("f₀ = " + Number(r.ResonanceFrequency) + " Гц",
                    Mathf.Clamp(x + 8, left, right - 205), topBottom - 23, 210, LabTheme.Voltage);
            }
            else Text("f₀ вне диапазона 1–1000 Гц", left + 160, topBottom - 23, 450, LabTheme.Voltage);
            float currentX = left + (float)(Math.Log10(p.Frequency) / 3) * width;
            Segment(currentX, low, currentX, high, new Color(.58f, .66f, .76f, .5f));
        }
    }
}
