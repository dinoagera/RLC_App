using System.Collections;
using System.Linq;
using NUnit.Framework;
using RLCLab.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RLCLab.Tests
{
    public sealed class LaboratoryTests
    {
        private static Button Button(string label) =>
            Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(b => b.name == "Button " + label);

        [UnityTest]
        public IEnumerator CompleteLaboratorySmokeTest()
        {
            SceneManager.LoadScene("Laboratory");
            yield return null;
            yield return null;
            var circuit = Object.FindFirstObjectByType<CircuitController>();
            var simulation = Object.FindFirstObjectByType<SimulationManager>();
            Assert.That(circuit, Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<Canvas>(), Is.Not.Null);
            Button("↺  Сброс").onClick.Invoke();
            Assert.That(circuit.Parameters.Frequency, Is.EqualTo(50));
            Assert.That(circuit.Result.Z, Is.EqualTo(100).Within(.01));
            Assert.That(Object.FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
            foreach (var slider in Object.FindObjectsByType<Slider>(FindObjectsSortMode.None))
            {
                Assert.That(slider.handleRect.rect.height, Is.LessThanOrEqualTo(32),
                    "The thumb must not stretch beyond its slider");
                Assert.That(slider.handleRect.rect.height, Is.GreaterThan(0));
            }
            Button("Ⅱ  Пауза").onClick.Invoke();
            Assert.That(simulation.IsRunning, Is.False);
            double phase = simulation.PhaseCycles;
            yield return null;
            Assert.That(simulation.PhaseCycles, Is.EqualTo(phase));
            Assert.That(circuit.Set(Parameter.Resistance, 20), Is.True);
            Assert.That(circuit.Result.Z, Is.LessThan(21), "Recalculation works while paused");
            var input = Object.FindFirstObjectByType<TMP_InputField>();
            var before = circuit.Parameters;
            input.onEndEdit.Invoke("NaN");
            Assert.That(circuit.Parameters.Resistance, Is.EqualTo(before.Resistance));
            Assert.That(circuit.TuneToResonance(), Is.True);
            Assert.That(circuit.Result.Phase, Is.EqualTo(0).Within(1e-10));
            Button("Фазовая диаграмма").onClick.Invoke();
            yield return null;
            Assert.That(Object.FindFirstObjectByType<PhasorDiagram>(), Is.Not.Null);
            Button("Частотный отклик").onClick.Invoke();
            yield return null;
            var graph = Object.FindFirstObjectByType<GraphRenderer>();
            Assert.That(graph.Mode, Is.EqualTo(GraphMode.Frequency));
            simulation.SetSpeed(4);
            Assert.That(circuit.Result.Phase, Is.EqualTo(0).Within(1e-10), "Speed does not change physical frequency");
            Button("▶  Старт").onClick.Invoke();
            Assert.That(simulation.IsRunning, Is.True);
            Button("↺  Сброс").onClick.Invoke();
            Assert.That(circuit.Parameters.Resistance, Is.EqualTo(100));
            Assert.That(simulation.Speed, Is.EqualTo(1));
            Assert.That(graph.Mode, Is.EqualTo(GraphMode.Time));
        }

        [UnityTest]
        public IEnumerator SettingsPersistAndCorruptedValuesRecover()
        {
            SceneManager.LoadScene("Laboratory");
            yield return null;
            yield return null;
            Button("↺  Сброс").onClick.Invoke();
            var circuit = Object.FindFirstObjectByType<CircuitController>();
            circuit.Set(Parameter.Resistance, 321.25);
            circuit.Set(Parameter.Inductance, 240);
            circuit.Set(Parameter.Capacitance, 330);
            circuit.Set(Parameter.Frequency, 77.5);
            circuit.Set(Parameter.Amplitude, 22);
            Object.FindFirstObjectByType<SimulationManager>().SetSpeed(2.5f);
            Button("Частотный отклик").onClick.Invoke();
            double expectedZ = circuit.Result.Z;
            circuit.Save();

            SceneManager.LoadScene("Laboratory");
            yield return null;
            yield return null;
            circuit = Object.FindFirstObjectByType<CircuitController>();
            Assert.That(circuit.Parameters.Resistance, Is.EqualTo(321.25));
            Assert.That(circuit.Parameters.Inductance, Is.EqualTo(240));
            Assert.That(circuit.Parameters.Capacitance, Is.EqualTo(330));
            Assert.That(circuit.Parameters.Frequency, Is.EqualTo(77.5));
            Assert.That(circuit.Parameters.Amplitude, Is.EqualTo(22));
            Assert.That(circuit.Result.Z, Is.EqualTo(expectedZ).Within(1e-10));
            Assert.That(Object.FindFirstObjectByType<SimulationManager>().Speed, Is.EqualTo(2.5f));
            Assert.That(Object.FindFirstObjectByType<GraphRenderer>().Mode, Is.EqualTo(GraphMode.Frequency));

            PlayerPrefs.SetString("rlc.Resistance", "NaN");
            PlayerPrefs.SetString("rlc.Frequency", "1001");
            PlayerPrefs.SetFloat("rlc.speed", float.NaN);
            PlayerPrefs.Save();
            SceneManager.LoadScene("Laboratory");
            yield return null;
            yield return null;
            circuit = Object.FindFirstObjectByType<CircuitController>();
            Assert.That(circuit.Parameters.Resistance, Is.EqualTo(100));
            Assert.That(circuit.Parameters.Frequency, Is.EqualTo(50));
            Assert.That(Object.FindFirstObjectByType<SimulationManager>().Speed, Is.EqualTo(1));
            Button("↺  Сброс").onClick.Invoke();
            circuit.Save();
        }
    }
}
