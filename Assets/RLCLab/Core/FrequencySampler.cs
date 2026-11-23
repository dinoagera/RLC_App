using System;
using System.Collections.Generic;

namespace RLCLab.Core
{
    public static class FrequencySampler
    {
        public static double[] Create(CircuitParameters p, int count = 560)
        {
            if (count < 2) throw new ArgumentOutOfRangeException(nameof(count));
            var frequencies = new SortedSet<double>();
            for (int i = 0; i < count; i++)
                frequencies.Add(Math.Pow(1000, (double)i / (count - 1)));
            var r = CircuitCalculator.Calculate(p);
            // A logarithmic grid alone can miss a narrow, high-Q resonance.
            // Add exact f0, current frequency and local bandwidth samples.
            frequencies.Add(p.Frequency);
            double bandwidth = p.Resistance / (2 * Math.PI * p.Inductance * 1e-3);
            for (int i = -48; i <= 48; i++)
            {
                double f = r.ResonanceFrequency + bandwidth * i / 16;
                if (f >= 1 && f <= 1000) frequencies.Add(f);
            }
            var result = new double[frequencies.Count];
            frequencies.CopyTo(result);
            return result;
        }
    }
}
