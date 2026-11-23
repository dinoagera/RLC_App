using System;
using System.Linq;
using System.Text.Json;
using RLCLab.Core;

static class Program
{
    private static int checks;
    static void Check(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception("FAIL: " + message);
    }
    static void Near(double actual, double expected, double tolerance, string message) =>
        Check(Math.Abs(actual - expected) <= tolerance, $"{message}: {actual} != {expected}");
    static int Main(string[] args)
    {
        var p = new CircuitParameters();
        var r = CircuitCalculator.Calculate(p);
        Near(r.Z, 100.0008613789517, 1e-9, "Reference Z");
        Near(r.PhaseDegrees, -0.23781168998129457, 1e-9, "Reference phase");
        Near(r.ResonanceFrequency, 50.32921210448704, 1e-11, "f0");
        Check(r.Phase < 0, "Below resonance: current leads");
        double f0 = r.ResonanceFrequency;
        p.Frequency = f0;
        r = CircuitCalculator.Calculate(p);
        Near(r.Z, p.Resistance, 1e-10, "At resonance Z = R");
        Near(r.Im, p.Amplitude / p.Resistance, 1e-12, "At resonance Im = Um/R");
        Near(r.Phase, 0, 1e-12, "At resonance phase zero");
        Check(ResonanceDetector.IsNear(f0 * .98, f0), "Lower boundary included");
        Check(ResonanceDetector.IsNear(f0 * 1.02, f0), "Upper boundary included");
        Check(!ResonanceDetector.IsNear(f0 * 1.02001, f0), "Outside boundary");
        Check(!ResonanceDetector.IsNear(0, 0), "Zero resonance rejected");
        p.Frequency = 100;
        Check(CircuitCalculator.Calculate(p).Phase > 0, "Above resonance: current lags");
        Check(CircuitParameters.TryParse(" 1,25 ", out double parsed) && parsed == 1.25, "Russian decimal");
        Check(CircuitParameters.TryParse("1.25", out parsed) && parsed == 1.25, "Invariant decimal");
        foreach (string bad in new[] { "", "abc", "NaN", "Infinity", "1e999", "1,2,3", "1 000" })
            Check(!CircuitParameters.TryParse(bad, out _), "Reject " + bad);
        var fixtures = new System.Collections.Generic.List<object>();
        // All 32 vertices of the parameter hypercube.
        for (int mask = 0; mask < 32; mask++)
        {
            var corner = new CircuitParameters();
            foreach (Parameter param in Enum.GetValues(typeof(Parameter)))
                corner[param] = (mask & (1 << (int)param)) != 0 ? CircuitParameters.Maximum(param) : 1;
            Validate(corner);
            fixtures.Add(Fixture(corner));
        }
        var random = new Random(32);
        for (int i = 0; i < 1500; i++)
        {
            var sample = new CircuitParameters();
            foreach (Parameter param in Enum.GetValues(typeof(Parameter)))
                sample[param] = 1 + random.NextDouble() * (CircuitParameters.Maximum(param) - 1);
            Validate(sample);
            if (i < 64) fixtures.Add(Fixture(sample));
        }
        var highQ = new CircuitParameters { Resistance = 1, Inductance = 500, Capacitance = 1 };
        var frequencies = FrequencySampler.Create(highQ);
        double exactF0 = CircuitCalculator.Calculate(highQ).ResonanceFrequency;
        Check(frequencies.Contains(exactF0), "Exact narrow resonance in sample set");
        Check(frequencies.Zip(frequencies.Skip(1), (a, b) => b > a).All(v => v), "Frequency samples strictly sorted");
        Near(frequencies.Max(f => highQ.Amplitude / CircuitCalculator.ImpedanceAt(highQ, f)), 10, 1e-10, "Narrow peak");
        var outside = new CircuitParameters { Inductance = 1, Capacitance = 1 };
        Check(CircuitCalculator.Calculate(outside).ResonanceFrequency > 1000, "Out-of-range f0");
        Check(FrequencySampler.Create(outside).All(f => f >= 1 && f <= 1000), "Sweep respects display range");
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity, 1001 })
        {
            var bad = new CircuitParameters { Frequency = invalid };
            Check(!bad.IsValid(), "Invalid parameter");
            bool thrown = false;
            try { CircuitCalculator.Calculate(bad); } catch (ArgumentOutOfRangeException) { thrown = true; }
            Check(thrown, "Calculator rejects invalid parameter");
        }
        if (args.Contains("--fixtures"))
            Console.WriteLine(JsonSerializer.Serialize(fixtures));
        else
            Console.WriteLine($"PASS: {checks} checks (control example, 32 corners, 1500 random circuits, resonance, validation, signals).");
        return 0;
    }
    static void Validate(CircuitParameters p)
    {
        var r = CircuitCalculator.Calculate(p);
        foreach (double x in new[] { r.Z, r.Im, r.XL, r.XC, r.Phase, r.ResonanceFrequency, r.Power })
            Check(CircuitParameters.IsFinite(x), "Finite result");
        Check(r.Z >= p.Resistance - 1e-10, "Z >= R");
        Check(r.Im <= p.Amplitude / p.Resistance + 1e-10, "Current <= resonant maximum");
        Near(r.UR * r.UR + (r.UL - r.UC) * (r.UL - r.UC), p.Amplitude * p.Amplitude,
            1e-7, "Voltage vector sum");
        Near(CircuitCalculator.ImpedanceAt(p, p.Frequency), r.Z, 1e-9, "Sweep agrees with calculator");
        double time = .123;
        Near(CircuitCalculator.Voltage(p, time), p.Amplitude * Math.Sin(r.Omega * time), 1e-10, "Voltage signal");
        Near(CircuitCalculator.Current(p, r, time), r.Im * Math.Sin(r.Omega * time - r.Phase), 1e-10, "Current signal");
    }
    static object Fixture(CircuitParameters p)
    {
        var r = CircuitCalculator.Calculate(p);
        return new { p = new { R = p.Resistance, L = p.Inductance, C = p.Capacitance, f = p.Frequency, U = p.Amplitude },
            expected = new { omega = r.Omega, xl = r.XL, xc = r.XC, z = r.Z, im = r.Im,
                phi = r.Phase, f0 = r.ResonanceFrequency, power = r.Power, q = r.Quality } };
    }
}
