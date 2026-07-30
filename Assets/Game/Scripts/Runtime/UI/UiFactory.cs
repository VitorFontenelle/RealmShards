using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Tiny runtime uGUI builder for placeholder screens (no prefab dependency).
    /// </summary>
    public static class UiFactory
    {
        public static Canvas CreateScreenCanvas(string name, int sortingOrder = 100)
        {
            var root = new GameObject(name);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Image AddPanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text AddText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Button AddButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var image = go.GetComponent<Image>();
            image.color = color ?? new Color(0.18f, 0.32f, 0.42f, 1f);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.28f, 0.45f, 0.55f, 1f);
            colors.pressedColor = new Color(0.12f, 0.22f, 0.30f, 1f);
            button.colors = colors;

            AddText(go.transform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return button;
        }
    }
}
