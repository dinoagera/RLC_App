using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace RLCLab
{
    public static class LabTheme
    {
        public static readonly Color Background = Hex("0B1220"), Panel = Hex("121E30"),
            Inset = Hex("0C1626"), Border = Hex("26364D"), Text = Hex("E8F0FA"),
            Muted = Hex("93A9C2"), Voltage = Hex("FBBF60"), Current = Hex("48DCC8"),
            Violet = Hex("AB9BFF"), Blue = Hex("65ADFF"), Red = Hex("FF8899");
        public static TMP_FontAsset Font;
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        // Top-left coordinates.
        public static void Place(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }
        public static void Fill(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom); rt.offsetMax = new Vector2(-right, -top);
        }
        public static Image Box(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }
        public static TextMeshProUGUI Label(Transform parent, string text, float size = 22, Color? color = null)
        {
            var rt = Rect("Text", parent);
            var label = rt.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font; label.text = text; label.fontSize = size;
            label.color = color ?? Text; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            return label;
        }
        public static TextMeshProUGUI TextAt(Transform parent, string text, float x, float y, float w, float h,
            float size = 22, Color? color = null)
        {
            var label = Label(parent, text, size, color);
            Place(label.rectTransform, x, y, w, h); return label;
        }
        public static Button Button(Transform parent, string text, UnityAction action, bool accent = false)
        {
            var image = Box("Button " + text, parent, accent ? Current : Border);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image; button.onClick.AddListener(action);
            var label = Label(image.transform, text, 21, accent ? Background : Text);
            label.enableAutoSizing = true;
            label.fontSizeMin = 16;
            label.fontSizeMax = 21;
            label.alignment = TextAlignmentOptions.Center; Fill(label.rectTransform, 8, 0, 8, 0);
            return button;
        }
        public static Slider Slider(Transform parent, float min, float max, UnityAction<float> action)
        {
            var root = Rect("Slider", parent);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = min; slider.maxValue = max;
            var bg = Box("Track", root, Border);
            Fill(bg.rectTransform, 0, 13, 0, 13); bg.raycastTarget = true;
            var area = Rect("Fill area", root); Fill(area, 0, 13, 0, 13);
            var fill = Box("Fill", area, Current); Fill(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            var handleArea = Rect("Handle area", root); Fill(handleArea, 8, 4, 8, 4);
            var handle = Box("Handle", handleArea, Text);
            // Slider stretches the cross-axis anchors: the height comes from the handle area.
            handle.rectTransform.sizeDelta = new Vector2(16, 0);
            handle.raycastTarget = true; slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle; slider.onValueChanged.AddListener(action);
            return slider;
        }
        public static TMP_InputField Input(Transform parent, UnityAction<string> action)
        {
            var image = Box("Numeric input", parent, Inset); image.raycastTarget = true;
            var input = image.gameObject.AddComponent<TMP_InputField>();
            var viewport = Rect("Viewport", image.transform); Fill(viewport, 12, 2, 12, 2);
            viewport.gameObject.AddComponent<RectMask2D>();
            var text = Label(viewport, "", 22); Fill(text.rectTransform);
            text.richText = false;
            input.textViewport = viewport; input.textComponent = text;
            input.fontAsset = Font;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 28;
            input.selectionColor = new Color(.28f, .86f, .78f, .25f);
            input.onEndEdit.AddListener(action);
            return input;
        }
    }
}
