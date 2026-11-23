using System;
using System.Globalization;
using RLCLab.Core;
using UnityEngine;

namespace RLCLab
{
    public sealed class CircuitController : MonoBehaviour
    {
        private CircuitParameters parameters = new CircuitParameters();
        public CircuitParameters Parameters => parameters.Copy();
        public double Frequency => parameters.Frequency;
        public CircuitResult Result { get; private set; }
        public event Action Changed;
        private bool pendingSave;

        public void Initialize()
        {
            var saved = new CircuitParameters();
            foreach (Parameter p in Enum.GetValues(typeof(Parameter)))
            {
                if (CircuitParameters.TryParse(PlayerPrefs.GetString("rlc." + p, ""), out double value)
                    && value >= 1 && value <= CircuitParameters.Maximum(p))
                    saved[p] = value;
            }
            parameters = saved;
            Recalculate();
        }

        public bool Set(Parameter p, double value)
        {
            if (!CircuitParameters.IsFinite(value) || value < 1 || value > CircuitParameters.Maximum(p))
                return false;
            parameters[p] = value;
            Recalculate();
            pendingSave = true;
            return true;
        }

        public bool SetText(Parameter p, string text, out string error)
        {
            error = "";
            if (!CircuitParameters.TryParse(text, out double value))
            {
                error = "Введите конечное число. Допустимы точка и запятая.";
                return false;
            }
            if (!Set(p, value))
            {
                error = "Допустимый диапазон: 1–" + CircuitParameters.Maximum(p) + ".";
                return false;
            }
            return true;
        }

        public void ResetParameters()
        {
            parameters = new CircuitParameters();
            Recalculate();
            pendingSave = true;
        }

        public bool TuneToResonance() => Set(Parameter.Frequency, Result.ResonanceFrequency);
        private void Recalculate()
        {
            Result = CircuitCalculator.Calculate(parameters);
            Changed?.Invoke();
        }

        // Disk writes are debounced; continuous dragging only changes in-memory settings.
        private float saveTimer;
        private void Update()
        {
            saveTimer += Time.unscaledDeltaTime;
            if (pendingSave && saveTimer >= 1) Save();
        }

        public void Save()
        {
            foreach (Parameter p in Enum.GetValues(typeof(Parameter)))
                PlayerPrefs.SetString("rlc." + p, parameters[p].ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
            pendingSave = false;
            saveTimer = 0;
        }
        private void OnApplicationQuit() => Save();
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
    }
}
