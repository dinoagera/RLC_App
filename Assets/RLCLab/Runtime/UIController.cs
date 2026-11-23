using System;
using System.Globalization;
using RLCLab.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RLCLab
{
    public sealed class UIController : MonoBehaviour
    {
        private CircuitController circuit;
        private SimulationManager simulation;
        private Canvas canvas;
        private TMP_FontAsset ownedFont;
        private readonly Slider[] sliders = new Slider[5];
        private readonly TMP_InputField[] inputs = new TMP_InputField[5];
        private readonly TextMeshProUGUI[] results = new TextMeshProUGUI[6];
        private readonly Button[] tabs = new Button[3];
        private GraphRenderer graph;
        private PhasorDiagram phasor;
        private CircuitSchematic schematic;
        private TextMeshProUGUI resonance, feedback, extraResults, graphNote, legend, playback, speedLabel;
        private Button normalizeButton, tuneButton;
        private Slider speedSlider;
        private GameObject help;
        private GraphMode mode;
        private bool normalized;
        private bool? lastRunning;
        private double lastFrequency = -1;
        private static readonly string[] Names =
            { "Сопротивление R", "Индуктивность L", "Ёмкость C", "Частота f", "Амплитуда Uₘ" };
        private static readonly string[] Units = { "Ом", "мГн", "мкФ", "Гц", "В" };
        private static string F(double n, string format = "0.###") => n.ToString(format, CultureInfo.InvariantCulture);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            circuit = gameObject.AddComponent<CircuitController>(); circuit.Initialize();
            simulation = gameObject.AddComponent<SimulationManager>(); simulation.Initialize();
            var sourceFont = Resources.Load<Font>("Fonts/LabSans");
            if (sourceFont == null) sourceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ownedFont = TMP_FontAsset.CreateFontAsset(sourceFont);
            LabTheme.Font = ownedFont;
            LabTheme.Font.name = "Lab Sans Dynamic";
            LabTheme.Font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            LabTheme.Font.isMultiAtlasTexturesEnabled = true;
            if (EventSystem.current == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Build();
            normalized = PlayerPrefs.GetInt("rlc.normalized", 1) == 1;
            int savedMode = Mathf.Clamp(PlayerPrefs.GetInt("rlc.mode", 0), 0, 2);
            SetMode((GraphMode)savedMode);
            circuit.Changed += Refresh;
            Refresh();
        }
        private void Build()
        {
            var canvasRoot = LabTheme.Rect("Laboratory Canvas", transform);
            canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.gameObject.AddComponent<GraphicRaycaster>();
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var bg = LabTheme.Box("Background", canvasRoot, LabTheme.Background); LabTheme.Fill(bg.rectTransform);
            BuildHeader(canvasRoot);

            var controls = LabTheme.Box("Parameters", canvasRoot, LabTheme.Panel);
            var cr = controls.rectTransform;
            cr.anchorMin = new Vector2(0, 0); cr.anchorMax = new Vector2(0, 1);
            cr.pivot = new Vector2(0, 1); cr.offsetMin = new Vector2(28, 106);
            cr.offsetMax = new Vector2(408, -100);
            BuildParameters(cr);

            var main = LabTheme.Rect("Workspace", canvasRoot);
            LabTheme.Fill(main, 428, 100, 28, 106);
            var schemeCard = LabTheme.Box("Circuit card", main, LabTheme.Panel);
            StretchTop(schemeCard.rectTransform, 0, 212);
            LabTheme.TextAt(schemeCard.transform, "01  /  ПОСЛЕДОВАТЕЛЬНАЯ ЦЕПЬ", 22, 10, 700, 32, 19, LabTheme.Muted);
            var schemeRect = LabTheme.Rect("Schematic mesh", schemeCard.transform);
            LabTheme.Fill(schemeRect, 15, 42, 15, 15);
            schematic = schemeRect.gameObject.AddComponent<CircuitSchematic>();
            schematic.raycastTarget = false; schematic.Circuit = circuit; schematic.Simulation = simulation;

            var resultCard = LabTheme.Box("Results", main, LabTheme.Panel);
            StretchTop(resultCard.rectTransform, 228, 126);
            string[] titles = { "|Z| · Ом", "Iₘ · А", "φ · град", "f₀ · Гц", "Xₗ · Ом", "X<sub>C</sub> · Ом" };
            for (int i = 0; i < 6; i++)
            {
                var cell = LabTheme.Rect(titles[i], resultCard.transform);
                cell.anchorMin = new Vector2(i / 6f, 0); cell.anchorMax = new Vector2((i + 1) / 6f, 1);
                cell.offsetMin = new Vector2(20, 35); cell.offsetMax = new Vector2(-8, -10);
                LabTheme.TextAt(cell, titles[i], 0, 0, 200, 28, 18, LabTheme.Muted);
                results[i] = LabTheme.TextAt(cell, "", 0, 28, 210, 42, 30,
                    i == 1 ? LabTheme.Current : i == 3 ? LabTheme.Voltage : LabTheme.Text);
            }
            extraResults = LabTheme.TextAt(resultCard.transform, "", 20, 95, 1350, 25, 17, LabTheme.Muted);

            var graphCard = LabTheme.Box("Graph card", main, LabTheme.Panel);
            LabTheme.Fill(graphCard.rectTransform, 0, 370, 0, 0);
            string[] tabNames = { "Временные сигналы", "Фазовая диаграмма", "Частотный отклик" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                tabs[i] = LabTheme.Button(graphCard.transform, tabNames[i], () => SetMode((GraphMode)index));
                LabTheme.Place((RectTransform)tabs[i].transform, 18 + i * 252, 16, 240, 44);
            }
            normalizeButton = LabTheme.Button(graphCard.transform, "Нормализация: вкл.", () =>
            {
                normalized = !normalized;
                PlayerPrefs.SetInt("rlc.normalized", normalized ? 1 : 0); RefreshGraph();
            });
            var nr = (RectTransform)normalizeButton.transform;
            nr.anchorMin = nr.anchorMax = new Vector2(1, 1); nr.pivot = new Vector2(1, 1);
            nr.anchoredPosition = new Vector2(-18, -16); nr.sizeDelta = new Vector2(260, 44);
            legend = LabTheme.TextAt(graphCard.transform, "", 24, 68, 1300, 28, 19);

            var plot = LabTheme.Rect("Oscilloscope mesh", graphCard.transform);
            LabTheme.Fill(plot, 20, 104, 20, 52);
            graph = plot.gameObject.AddComponent<GraphRenderer>();
            graph.raycastTarget = false; graph.Circuit = circuit; graph.Simulation = simulation;
            var pr = LabTheme.Rect("Phasor mesh", graphCard.transform); LabTheme.Fill(pr, 20, 104, 20, 52);
            phasor = pr.gameObject.AddComponent<PhasorDiagram>();
            phasor.raycastTarget = false; phasor.Circuit = circuit;
            graphNote = LabTheme.Label(graphCard.transform, "", 17, LabTheme.Muted);
            var note = graphNote.rectTransform;
            note.anchorMin = Vector2.zero; note.anchorMax = new Vector2(1, 0);
            note.offsetMin = new Vector2(24, 10); note.offsetMax = new Vector2(-24, 44);
            BuildFooter(canvasRoot);
            BuildHelp(canvasRoot);
        }
        private static void StretchTop(RectTransform rt, float y, float h)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(.5f, 1); rt.offsetMin = new Vector2(0, -y - h);
            rt.offsetMax = new Vector2(0, -y);
        }
        private void BuildHeader(Transform parent)
        {
            LabTheme.TextAt(parent, "RLC / LAB", 30, 12, 220, 30, 23, LabTheme.Current).fontStyle = FontStyles.Bold;
            LabTheme.TextAt(parent, "Лаборатория переменного тока", 255, 10, 730, 38, 30);
            LabTheme.TextAt(parent, "Установившийся синусоидальный режим  /  Виртуальный эксперимент №32",
                255, 49, 1050, 28, 17, LabTheme.Muted);
            resonance = LabTheme.Label(parent, "", 22, LabTheme.Current);
            var rr = resonance.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(1, 1); rr.pivot = new Vector2(1, 1);
            rr.anchoredPosition = new Vector2(-205, -23); rr.sizeDelta = new Vector2(510, 42);
            resonance.alignment = TextAlignmentOptions.Right;
            var helpButton = LabTheme.Button(parent, "?  Справка", () => help.SetActive(true));
            var hr = (RectTransform)helpButton.transform;
            hr.anchorMin = hr.anchorMax = new Vector2(1, 1); hr.pivot = new Vector2(1, 1);
            hr.anchoredPosition = new Vector2(-28, -23); hr.sizeDelta = new Vector2(150, 44);
        }
        private void BuildParameters(Transform parent)
        {
            LabTheme.TextAt(parent, "Параметры эксперимента", 20, 18, 345, 36, 25);
            LabTheme.TextAt(parent, "Изменения применяются сразу", 20, 54, 345, 28, 18, LabTheme.Muted);
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                Parameter parameter = (Parameter)i;
                var row = LabTheme.Rect(Names[i], parent);
                LabTheme.Place(row, 20, 100 + i * 103, 340, 96);
                LabTheme.TextAt(row, Names[i], 0, 0, 206, 36, 21);
                inputs[i] = LabTheme.Input(row, text =>
                {
                    // Focusing then leaving a rounded display must not round the actual f0.
                    if (text == F(circuit.Parameters[parameter], "0.#########")) return;
                    if (!circuit.SetText(parameter, text, out string error))
                    {
                        feedback.text = error; feedback.color = LabTheme.Red;
                        inputs[index].SetTextWithoutNotify(F(circuit.Parameters[parameter], "0.#########"));
                    }
                    else { feedback.text = "Параметры обновлены"; feedback.color = LabTheme.Current; }
                });
                LabTheme.Place((RectTransform)inputs[i].transform, 210, 0, 130, 38);
                sliders[i] = LabTheme.Slider(row, 1, (float)CircuitParameters.Maximum(parameter),
                    value =>
                    {
                        circuit.Set(parameter, value);
                        feedback.text = "Параметры обновлены"; feedback.color = LabTheme.Current;
                    });
                LabTheme.Place((RectTransform)sliders[i].transform, 0, 43, 340, 32);
                LabTheme.TextAt(row, "1 " + Units[i], 0, 74, 150, 22, 16, LabTheme.Muted);
                var maximum = LabTheme.TextAt(row, CircuitParameters.Maximum(parameter) + " " + Units[i],
                    180, 74, 160, 22, 16, LabTheme.Muted);
                maximum.alignment = TextAlignmentOptions.Right;
            }
            feedback = LabTheme.TextAt(parent, "Настройки сохраняются автоматически", 20, 621, 340, 44, 17, LabTheme.Muted);
            feedback.textWrappingMode = TextWrappingModes.Normal;
            tuneButton = LabTheme.Button(parent, "Настроить резонанс · f = f₀", () =>
            {
                if (!circuit.TuneToResonance())
                {
                    feedback.text = "f₀ вне диапазона источника 1–1000 Гц.";
                    feedback.color = LabTheme.Voltage;
                }
            }, true);
            LabTheme.Place((RectTransform)tuneButton.transform, 20, 674, 340, 46);
            LabTheme.TextAt(parent, "ГОТОВЫЕ ЭКСПЕРИМЕНТЫ", 20, 735, 340, 28, 17, LabTheme.Muted);
            var capacitive = LabTheme.Button(parent, "Ток опережает", () => Preset(false));
            LabTheme.Place((RectTransform)capacitive.transform, 20, 772, 164, 44);
            var inductive = LabTheme.Button(parent, "Ток отстаёт", () => Preset(true));
            LabTheme.Place((RectTransform)inductive.transform, 196, 772, 164, 44);
        }
        private void Preset(bool inductive)
        {
            circuit.ResetParameters();
            circuit.Set(Parameter.Resistance, 30);
            circuit.Set(Parameter.Frequency, inductive ? 150 : 20);
        }
        private void BuildFooter(Transform parent)
        {
            var footer = LabTheme.Rect("Playback", parent);
            footer.anchorMin = Vector2.zero; footer.anchorMax = new Vector2(1, 0);
            footer.offsetMin = new Vector2(28, 20); footer.offsetMax = new Vector2(-28, 88);
            var start = LabTheme.Button(footer, "▶  Старт", () => simulation.SetRunning(true), true);
            LabTheme.Place((RectTransform)start.transform, 0, 8, 136, 48);
            var pause = LabTheme.Button(footer, "Ⅱ  Пауза", () => simulation.SetRunning(false));
            LabTheme.Place((RectTransform)pause.transform, 148, 8, 136, 48);
            var reset = LabTheme.Button(footer, "↺  Сброс", ResetAll);
            LabTheme.Place((RectTransform)reset.transform, 296, 8, 136, 48);
            speedLabel = LabTheme.TextAt(footer, "", 470, 2, 250, 28, 19);
            speedSlider = LabTheme.Slider(footer, .1f, 4, value =>
            {
                simulation.SetSpeed(value); speedLabel.text = "Скорость · " + F(value, "0.0") + "×";
            });
            LabTheme.Place((RectTransform)speedSlider.transform, 470, 32, 250, 32);
            speedSlider.SetValueWithoutNotify(simulation.Speed);
            speedLabel.text = "Скорость · " + F(simulation.Speed, "0.0") + "×";
            playback = LabTheme.TextAt(footer, "", 760, 6, 1080, 30, 19, LabTheme.Current);
            LabTheme.TextAt(footer, "Пробел — старт / пауза    R — сброс    1 / 2 / 3 — режим    F1 — справка",
                760, 34, 1080, 27, 17, LabTheme.Muted);
        }
        private void BuildHelp(Transform parent)
        {
            var overlay = LabTheme.Box("Help overlay", parent, new Color(0, 0, 0, .82f));
            LabTheme.Fill(overlay.rectTransform); overlay.raycastTarget = true; help = overlay.gameObject;
            var card = LabTheme.Box("Guide", overlay.transform, LabTheme.Panel);
            var rt = card.rectTransform; rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f); rt.sizeDelta = new Vector2(1130, 900);
            LabTheme.TextAt(rt, "Как устроена RLC-лаборатория", 38, 24, 960, 55, 32, LabTheme.Current);
            var scrollRoot = LabTheme.Rect("Guide scroll", rt);
            LabTheme.Place(scrollRoot, 38, 95, 1050, 706);
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            var viewport = LabTheme.Box("Guide viewport", scrollRoot, LabTheme.Panel);
            LabTheme.Fill(viewport.rectTransform); viewport.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport.rectTransform;
            var text = LabTheme.TextAt(viewport.transform,
                "1. Изменяйте R, L, C, частоту f и амплитуду Uₘ ползунками или вводом числа. " +
                "Enter применяет ввод; допустимы точка и запятая. Значения вне диапазона отклоняются.\n\n" +
                "2. Импеданс |Z| = √(R² + (Xₗ − X<sub>C</sub>)²) — полное сопротивление цепи. " +
                "Xₗ = 2πfL растёт с частотой, X<sub>C</sub> = 1/(2πfC) уменьшается. В формулах L — в Гн, C — в Ф.\n\n" +
                "3. Iₘ = Uₘ / |Z| — амплитуда тока. Uₘ и Iₘ — пиковые, не действующие значения. " +
                "Действующие значения меньше в √2 раз. P = Iₘ²R/2 — активная мощность.\n\n" +
                "4. φ = atan2(Xₗ − X<sub>C</sub>, R). При φ > 0 ток отстаёт от напряжения; при φ < 0 — опережает. " +
                "u(t) = Uₘ sin(ωt), i(t) = Iₘ sin(ωt − φ).\n\n" +
                "5. Временные графики показывают три периода на общей оси t. Без нормализации " +
                "используется общая числовая шкала (напряжение — В, ток — А). Нормализация делит " +
                "каждый сигнал на его амплитуду и помогает сравнивать фазы.\n\n" +
                "6. Фазовая диаграмма: ток принят за опорное направление вправо. Uᵣ совпадает по фазе " +
                "с током, Uₗ направлено вверх, U<sub>C</sub> — вниз, U — их векторная сумма. " +
                "Напряжения имеют единый масштаб; ток вынесен на отдельную опорную ось.\n\n" +
                "7. Резонанс f₀ = 1/(2π√LC): Xₗ = X<sub>C</sub>, |Z| = R, Iₘ = Uₘ/R, φ = 0. " +
                "Индикатор включается в пределах ±2% от f₀. Кнопка настройки использует точное f₀, " +
                "если оно находится в диапазоне 1–1000 Гц. На АЧХ пунктир — f₀, серая линия — текущая f.\n\n" +
                "8. Старт и пауза управляют только анимацией. Скорость — замедленная развёртка " +
                "(0,35 периода/с при 1×), она НЕ меняет частоту источника. Сброс возвращает " +
                "R=100 Ом, L=100 мГн, C=100 мкФ, f=50 Гц, Uₘ=10 В и скорость 1×.\n\n" +
                "Модель: только последовательная линейная RLC-цепь в установившемся синусоидальном " +
                "режиме. Переходные процессы не моделируются. Настройки сохраняются между запусками.",
                0, 0, 1018, 706, 22, LabTheme.Text);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.verticalAlignment = VerticalAlignmentOptions.Top;
            var content = text.rectTransform;
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(-24, text.GetPreferredValues(text.text, 1026, Mathf.Infinity).y + 24);
            scroll.content = content;
            LabTheme.TextAt(rt, "Прокрутка: колесо мыши", 790, 38, 310, 32, 17, LabTheme.Muted);
            var close = LabTheme.Button(rt, "Понятно  /  Esc", () => help.SetActive(false), true);
            LabTheme.Place((RectTransform)close.transform, 38, 822, 1050, 48);
            help.SetActive(false);
        }
        private void SetMode(GraphMode value)
        {
            mode = value; PlayerPrefs.SetInt("rlc.mode", (int)mode);
            RefreshGraph();
        }
        private void RefreshGraph()
        {
            graph.Mode = mode; graph.Normalized = normalized; graph.Invalidate();
            graph.gameObject.SetActive(mode != GraphMode.Phasor);
            phasor.gameObject.SetActive(mode == GraphMode.Phasor); phasor.Invalidate();
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].GetComponent<Image>().color = i == (int)mode ? LabTheme.Current : LabTheme.Border;
                tabs[i].GetComponentInChildren<TextMeshProUGUI>().color = i == (int)mode ? LabTheme.Background : LabTheme.Text;
            }
            normalizeButton.gameObject.SetActive(mode == GraphMode.Time);
            normalizeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Нормализация: " + (normalized ? "вкл." : "выкл.");
            if (mode == GraphMode.Time)
            {
                legend.text = "<color=#FBBF60>━━  u(t), напряжение</color>      <color=#48DCC8>┄  i(t), ток</color>";
                graphNote.text = normalized ? "Нормированные амплитуды · 3 периода · развёртка замедлена для наблюдения"
                    : "Общая числовая шкала: u в вольтах, i в амперах · для сравнения фаз включите нормализацию";
            }
            else if (mode == GraphMode.Phasor)
            {
                legend.text = "<color=#FBBF60>━━  U</color>    <color=#48DCC8>━━  I</color>    <color=#93A9C2>━━  Uᵣ</color>" +
                    "    <color=#65ADFF>━━  Uₗ</color>    <color=#AB9BFF>━━  U<sub>C</sub></color>";
                graphNote.text = "Ток — опорная фаза 0° · напряжения в едином масштабе · φ > 0: ток отстаёт";
            }
            else
            {
                legend.text = "<color=#48DCC8>━━  Iₘ(f)</color>      <color=#AB9BFF>━━  |Z(f)|</color>      <color=#FBBF60>┄  f₀</color>";
                graphNote.text = "Диапазон 1–1000 Гц · независимые вертикальные шкалы · точный пик резонанса включён в выборку";
            }
        }
        private void Refresh()
        {
            var p = circuit.Parameters; var r = circuit.Result;
            for (int i = 0; i < 5; i++)
            {
                sliders[i].SetValueWithoutNotify((float)p[(Parameter)i]);
                inputs[i].SetTextWithoutNotify(F(p[(Parameter)i], "0.#########"));
            }
            results[0].text = F(r.Z, "0.00");
            results[1].text = F(r.Im, "0.0000");
            results[2].text = F(r.PhaseDegrees, "+0.00;−0.00;0.00") + "°";
            results[3].text = F(r.ResonanceFrequency, "0.000");
            results[4].text = F(r.XL, "0.00"); results[5].text = F(r.XC, "0.00");
            bool near = ResonanceDetector.IsNear(p.Frequency, r.ResonanceFrequency);
            resonance.text = near ? "●  Область резонанса" : r.Phase >= 0 ? "●  Ток отстаёт от напряжения" : "●  Ток опережает напряжение";
            resonance.color = near ? LabTheme.Current : LabTheme.Muted;
            extraResults.text = "ω = " + F(r.Omega, "0.00") + " рад/с     P = " + F(r.Power) +
                " Вт     cos φ = " + F(r.PowerFactor) + "     Q = " + F(r.Quality) +
                "     Iдейств = " + F(r.Im / Math.Sqrt(2), "0.0000") + " А";
            tuneButton.interactable = r.ResonanceFrequency >= 1 && r.ResonanceFrequency <= 1000;
            schematic.Invalidate(); graph.Invalidate(); phasor.Invalidate();
        }
        private void ResetAll()
        {
            // Release focused input before resetting so its old end-edit does not restore a stale value.
            EventSystem.current.SetSelectedGameObject(null);
            circuit.ResetParameters(); simulation.ResetSimulation();
            speedSlider.SetValueWithoutNotify(1); speedLabel.text = "Скорость · 1.0×";
            normalized = true; SetMode(GraphMode.Time); PlayerPrefs.SetInt("rlc.normalized", 1);
            feedback.text = "Исходные настройки восстановлены"; feedback.color = LabTheme.Current;
        }
        private void Update()
        {
            if (lastRunning != simulation.IsRunning || lastFrequency != circuit.Frequency)
            {
                lastRunning = simulation.IsRunning; lastFrequency = circuit.Frequency;
                playback.text = (simulation.IsRunning ? "●  Анимация запущена" : "Ⅱ  Анимация на паузе") +
                    "   ·   Частота источника " + F(circuit.Frequency) + " Гц";
            }
            if (Input.GetKeyDown(KeyCode.Escape)) help.SetActive(false);
            if (Input.GetKeyDown(KeyCode.F1)) help.SetActive(!help.activeSelf);
            var selected = EventSystem.current.currentSelectedGameObject;
            bool typing = selected != null && selected.GetComponent<TMP_InputField>() != null;
            if (typing || help.activeSelf) return;
            if (Input.GetKeyDown(KeyCode.Space)) simulation.SetRunning(!simulation.IsRunning);
            if (Input.GetKeyDown(KeyCode.R)) ResetAll();
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(GraphMode.Time);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(GraphMode.Phasor);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(GraphMode.Frequency);
        }
        private void OnDestroy()
        {
            if (circuit != null) circuit.Changed -= Refresh;
            if (ownedFont != null) Destroy(ownedFont);
            if (LabTheme.Font == ownedFont) LabTheme.Font = null;
        }
    }
}
