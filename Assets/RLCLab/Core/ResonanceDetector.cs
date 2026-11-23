using System;

namespace RLCLab.Core
{
    public static class ResonanceDetector
    {
        public static bool IsNear(double frequency, double resonanceFrequency) =>
            CircuitParameters.IsFinite(frequency) && CircuitParameters.IsFinite(resonanceFrequency)
            && frequency > 0 && resonanceFrequency > 0
            && Math.Abs(frequency - resonanceFrequency) / resonanceFrequency <= 0.02 + 1e-14;
    }
}
