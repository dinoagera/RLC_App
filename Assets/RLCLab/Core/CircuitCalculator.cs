using System;

namespace RLCLab.Core
{
    public readonly struct CircuitResult
    {
        public readonly double Omega, XL, XC, Z, Im, Phase, ResonanceFrequency;
        public readonly double UR, UL, UC, Power, PowerFactor, Quality;
        public double PhaseDegrees => Phase * 180 / Math.PI;
        public CircuitResult(CircuitParameters p)
        {
            Omega = 2 * Math.PI * p.Frequency;
            double l = p.Inductance * 1e-3, c = p.Capacitance * 1e-6;
            XL = Omega * l;
            XC = 1 / (Omega * c);
            Z = Math.Sqrt(p.Resistance * p.Resistance + (XL - XC) * (XL - XC));
            Im = p.Amplitude / Z;
            Phase = Math.Atan2(XL - XC, p.Resistance);
            ResonanceFrequency = 1 / (2 * Math.PI * Math.Sqrt(l * c));
            UR = Im * p.Resistance;
            UL = Im * XL;
            UC = Im * XC;
            Power = Im * Im * p.Resistance / 2;
            PowerFactor = p.Resistance / Z;
            Quality = Math.Sqrt(l / c) / p.Resistance;
        }
    }

    /// <summary>Pure steady-state calculation; has no Unity or scene dependencies.</summary>
    public static class CircuitCalculator
    {
        public static CircuitResult Calculate(CircuitParameters parameters)
        {
            if (parameters == null || !parameters.IsValid())
                throw new ArgumentOutOfRangeException(nameof(parameters), "Параметры вне допустимых диапазонов.");
            return new CircuitResult(parameters);
        }

        // Frequency sweeps may include f0 outside the editable source-frequency range.
        public static double ImpedanceAt(CircuitParameters p, double frequency)
        {
            if (p == null || !p.IsValid() || !CircuitParameters.IsFinite(frequency) || frequency <= 0)
                throw new ArgumentOutOfRangeException(nameof(frequency));
            double omega = 2 * Math.PI * frequency;
            double x = omega * p.Inductance * 1e-3 - 1 / (omega * p.Capacitance * 1e-6);
            return Math.Sqrt(p.Resistance * p.Resistance + x * x);
        }

        public static double Voltage(CircuitParameters p, double time) =>
            p.Amplitude * Math.Sin(2 * Math.PI * p.Frequency * time);
        public static double Current(CircuitParameters p, CircuitResult r, double time) =>
            r.Im * Math.Sin(r.Omega * time - r.Phase);
    }
}
