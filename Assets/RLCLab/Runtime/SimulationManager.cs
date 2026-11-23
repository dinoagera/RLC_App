using UnityEngine;

namespace RLCLab
{
    public sealed class SimulationManager : MonoBehaviour
    {
        public bool IsRunning { get; private set; } = true;
        public double PhaseCycles { get; private set; }
        public float Speed { get; private set; } = 1;
        public void SetRunning(bool running) => IsRunning = running;
        public void SetSpeed(float speed)
        {
            if (float.IsNaN(speed) || float.IsInfinity(speed)) speed = 1;
            Speed = Mathf.Clamp(speed, .1f, 4);
            PlayerPrefs.SetFloat("rlc.speed", Speed);
        }
        public void Initialize() => SetSpeed(PlayerPrefs.GetFloat("rlc.speed", 1));
        public void ResetSimulation()
        {
            PhaseCycles = 0;
            SetSpeed(1);
            IsRunning = true;
        }
        private void Update()
        {
            // Slow-motion oscilloscope: 0.35 displayed cycles/s at 1x.
            // This affects ONLY the viewport, never omega or the circuit parameters.
            if (IsRunning) PhaseCycles = (PhaseCycles + Time.unscaledDeltaTime * Speed * .35) % 1000;
        }
    }
}
