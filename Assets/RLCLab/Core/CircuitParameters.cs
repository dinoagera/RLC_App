using System;
using System.Globalization;

namespace RLCLab.Core
{
    public enum Parameter { Resistance, Inductance, Capacitance, Frequency, Amplitude }

    /// <summary>UI units: ohms, millihenries, microfarads, hertz, peak volts.</summary>
    [Serializable]
    public sealed class CircuitParameters
    {
        public double Resistance = 100;
        public double Inductance = 100;
        public double Capacitance = 100;
        public double Frequency = 50;
        public double Amplitude = 10;

        public double this[Parameter parameter]
        {
            get
            {
                switch (parameter)
                {
                    case Parameter.Resistance: return Resistance;
                    case Parameter.Inductance: return Inductance;
                    case Parameter.Capacitance: return Capacitance;
                    case Parameter.Frequency: return Frequency;
                    default: return Amplitude;
                }
            }
            set
            {
                switch (parameter)
                {
                    case Parameter.Resistance: Resistance = value; break;
                    case Parameter.Inductance: Inductance = value; break;
                    case Parameter.Capacitance: Capacitance = value; break;
                    case Parameter.Frequency: Frequency = value; break;
                    default: Amplitude = value; break;
                }
            }
        }

        public CircuitParameters Copy() => (CircuitParameters)MemberwiseClone();
        public static double Minimum(Parameter parameter) => 1;
        public static double Maximum(Parameter parameter)
        {
            if (parameter == Parameter.Inductance) return 500;
            return parameter == Parameter.Amplitude ? 100 : 1000;
        }

        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public bool IsValid()
        {
            return InRange(Resistance, 1000) && InRange(Inductance, 500)
                && InRange(Capacitance, 1000) && InRange(Frequency, 1000) && InRange(Amplitude, 100);
        }
        private static bool InRange(double value, double max) => IsFinite(value) && value >= 1 && value <= max;

        public static bool TryParse(string text, out double value)
        {
            // Deliberately disallow thousands separators: "1,5" is 1.5 in either locale.
            return double.TryParse((text ?? "").Trim().Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out value) && IsFinite(value);
        }
    }
}
