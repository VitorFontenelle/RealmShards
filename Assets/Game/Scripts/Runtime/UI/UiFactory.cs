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
            UiScaleConfig.Apply(scaler);
            root.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static CanvasGroup AddCanvasGroup(GameObject go, float alpha = 1f, bool interactable = true, bool blocksRaycasts = true)
        {
            var group = go.GetComponent<CanvasGroup>();
            if (group == null)
                group = go.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.interactable = interactable;
            group.blocksRaycasts = blocksRaycasts;
            return group;
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

        public static Text AddText(Transform parent, string name, string content, int fontSize, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Font font = null)
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
            text.font = font ?? UiFonts.MenuRegular;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static Image AddSprite(Transform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool preserveAspect = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return image;
        }

        public static Button AddButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color? color = null, Font font = null)
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
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font);

            return button;
        }

        public static Button AddMenuTextButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color normalColor, Color highlightColor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.16f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.12f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var text = labelGo.GetComponent<Text>();
            text.text = label.ToUpperInvariant();
            text.font = UiFonts.MenuBold;
            text.fontSize = 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = normalColor;
            text.raycastTarget = false;

            var outline = labelGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.25f, 0.95f, 0.35f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var shadow = labelGo.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(0f, -2f);

            var hover = labelGo.AddComponent<MenuTextButtonHover>();
            hover.Initialize(text, outline, normalColor, highlightColor);

            return button;
        }

        private sealed class MenuTextButtonHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.ISelectHandler, UnityEngine.EventSystems.IDeselectHandler
        {
            private Text _text;
            private Outline _outline;
            private Color _normal;
            private Color _highlight;

            public void Initialize(Text text, Outline outline, Color normal, Color highlight)
            {
                _text = text;
                _outline = outline;
                _normal = normal;
                _highlight = highlight;
            }

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData) => ApplyHighlight();
            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData) => ApplyNormal();
            public void OnSelect(UnityEngine.EventSystems.BaseEventData eventData) => ApplyHighlight();
            public void OnDeselect(UnityEngine.EventSystems.BaseEventData eventData) => ApplyNormal();

            private void ApplyHighlight()
            {
                if (_text != null) _text.color = _highlight;
                if (_outline != null) _outline.effectColor = new Color(0.75f, 0.45f, 1f, 0.85f);
            }

            private void ApplyNormal()
            {
                if (_text != null) _text.color = _normal;
                if (_outline != null) _outline.effectColor = new Color(0.55f, 0.25f, 0.95f, 0.35f);
            }
        }
    }
}
