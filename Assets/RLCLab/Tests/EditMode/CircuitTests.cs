using System;
using System.Linq;
using NUnit.Framework;
using RLCLab.Core;

namespace RLCLab.Tests
{
    public sealed class CircuitTests
    {
        [Test]
        public void SpecificationControlExample()
        {
            var r = CircuitCalculator.Calculate(new CircuitParameters());
            Assert.That(r.Z, Is.EqualTo(100).Within(.01));
            Assert.That(r.PhaseDegrees, Is.EqualTo(-.24).Within(.005));
            Assert.That(r.ResonanceFrequency, Is.EqualTo(50.329212104487).Within(1e-9));
        }
        [Test]
        public void ExactResonance()
        {
            var p = new CircuitParameters();
            p.Frequency = CircuitCalculator.Calculate(p).ResonanceFrequency;
            var r = CircuitCalculator.Calculate(p);
            Assert.That(r.Z, Is.EqualTo(p.Resistance).Within(1e-10));
            Assert.That(r.Phase, Is.EqualTo(0).Within(1e-10));
            Assert.That(r.Im, Is.EqualTo(p.Amplitude / p.Resistance).Within(1e-10));
        }
        [TestCase(.98, true)]
        [TestCase(1.02, true)]
        [TestCase(1.02001, false)]
        [TestCase(.97999, false)]
        public void ResonanceBoundaries(double factor, bool expected) =>
            Assert.That(ResonanceDetector.IsNear(50 * factor, 50), Is.EqualTo(expected));
        [TestCase("1,5", true)]
        [TestCase("1.5", true)]
        [TestCase("NaN", false)]
        [TestCase("Infinity", false)]
        [TestCase("", false)]
        [TestCase("abc", false)]
        public void InputFormat(string text, bool valid) =>
            Assert.That(CircuitParameters.TryParse(text, out _), Is.EqualTo(valid));
        [Test]
        public void EveryExtremeCombination()
        {
            for (int mask = 0; mask < 32; mask++)
            {
                var p = new CircuitParameters();
                foreach (Parameter param in Enum.GetValues(typeof(Parameter)))
                    p[param] = (mask & (1 << (int)param)) != 0 ? CircuitParameters.Maximum(param) : 1;
                var r = CircuitCalculator.Calculate(p);
                Assert.That(CircuitParameters.IsFinite(r.Z) && CircuitParameters.IsFinite(r.Im), Is.True);
                Assert.That(r.Z, Is.GreaterThanOrEqualTo(p.Resistance - 1e-9));
                Assert.That(r.Im, Is.LessThanOrEqualTo(p.Amplitude / p.Resistance + 1e-9));
            }
        }
        [Test]
        public void InvalidModelIsRejected() =>
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CircuitCalculator.Calculate(new CircuitParameters { Resistance = 0 }));
        [Test]
        public void HighQPeakIsNotMissed()
        {
            var p = new CircuitParameters { Resistance = 1, Inductance = 500, Capacitance = 1 };
            var f = FrequencySampler.Create(p);
            Assert.That(f.Contains(CircuitCalculator.Calculate(p).ResonanceFrequency), Is.True);
            Assert.That(f.Max(x => p.Amplitude / CircuitCalculator.ImpedanceAt(p, x)), Is.EqualTo(10).Within(1e-9));
        }
    }
}
