using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Input;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Full-screen options panel: video, audio, gameplay, and system settings.
    /// </summary>
    public sealed class OptionsScreen : MonoBehaviour
    {
        private GameObject _root;
        private Text _status;
        private readonly List<RowBinding> _rows = new List<RowBinding>();
        private ControlsRebindScreen _controls;

        private struct RowBinding
        {
            public string Key;
            public Text ValueText;
            public Image CheckboxFill;
            public Slider Slider;
        }

        public static OptionsScreen EnsurePresent(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<OptionsScreen>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(OptionsScreen));
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<OptionsScreen>();
            screen.Build();
            screen.Hide();
            return screen;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("OptionsUI", 300);
            canvas.transform.SetParent(transform, false);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());

            _root = canvas.gameObject;

            UiFactory.AddPanel(canvas.transform, "Dim", new Color(0.02f, 0.03f, 0.05f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvas.transform, false);
            var safeRt = safe.GetComponent<RectTransform>();
            UiScaleConfig.ApplySafeArea(safeRt);

            var box = new GameObject("OptionsBox", typeof(RectTransform));
            box.transform.SetParent(safe.transform, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.18f, 0.05f);
            boxRt.anchorMax = new Vector2(0.82f, 0.95f);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;

            UiFactory.AddPanel(box.transform, "Border", new Color(0.92f, 0.92f, 0.95f, 0.95f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.AddPanel(box.transform, "Background", new Color(0.08f, 0.08f, 0.10f, 0.94f),
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            UiFactory.AddText(box.transform, "Title", "OPTIONS", 34, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f), Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(box.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.05f, 0.10f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.90f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
            viewport.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 760f);

            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            float y = 0f;
            const float rowH = 34f;
            const float headerH = 30f;
            const float gap = 4f;

            y = AddHeader(content.transform, "- VIDEO -", y, headerH);
            y = AddCycleRow(content.transform, "resolution", "RESOLUTION", y, rowH, () => SettingsService.GetResolutionLabel(), delta => SettingsService.CycleResolution(delta));
            y = AddToggleRow(content.transform, "fullscreen", "FULLSCREEN", y, rowH, () => SettingsService.Data.fullscreen, SettingsService.ToggleFullscreen);
            y = AddSliderRow(content.transform, "brightness", "BRIGHTNESS", y, rowH, () => SettingsService.Data.brightness, SettingsService.SetBrightness);
            y = AddToggleRow(content.transform, "vsync", "V-SYNC", y, rowH, () => SettingsService.Data.vSync, SettingsService.ToggleVSync);
            y = AddToggleRow(content.transform, "cursor", "SYSTEM CURSOR", y, rowH, () => SettingsService.Data.systemCursor, SettingsService.ToggleSystemCursor);
            y = AddToggleRow(content.transform, "vfx", "VISUAL EFFECTS", y, rowH, () => SettingsService.Data.visualEffects, SettingsService.ToggleVisualEffects);

            y = AddHeader(content.transform, "- AUDIO -", y + gap, headerH);
            y = AddSliderRow(content.transform, "music", "MUSIC", y, rowH, () => SettingsService.Data.musicVolume, SettingsService.SetMusicVolume);
            y = AddSliderRow(content.transform, "sfx", "SFX", y, rowH, () => SettingsService.Data.sfxVolume, SettingsService.SetSfxVolume);

            y = AddHeader(content.transform, "- GAMEPLAY -", y + gap, headerH);
            y = AddSliderRow(content.transform, "shake", "CAMERA SHAKE", y, rowH, () => SettingsService.Data.cameraShake, SettingsService.SetCameraShake);
            y = AddSliderRow(content.transform, "minimap", "MINIMAP SIZE", y, rowH, () => (SettingsService.Data.minimapSize - 0.6f) / 1f, v => SettingsService.SetMinimapSize(0.6f + v * 1f));
            y = AddToggleRow(content.transform, "numbers", "DISPLAY NUMBERS", y, rowH, () => SettingsService.Data.displayNumbers, SettingsService.ToggleDisplayNumbers);
            y = AddCycleRow(content.transform, "controller", "CONTROLLER BUTTONS", y, rowH,
                () => SettingsService.ControllerButtonTypes[SettingsService.Data.controllerButtonType],
                delta => SettingsService.CycleControllerButtons(delta));

            y = AddHeader(content.transform, "- SYSTEM -", y + gap, headerH);
            y = AddCycleRow(content.transform, "language", "LANGUAGE", y, rowH,
                () => SettingsService.Languages[SettingsService.Data.languageIndex],
                delta => SettingsService.CycleLanguage(delta));
            y = AddActionRow(content.transform, "KEY CONFIG", y, rowH, OpenKeyConfig);
            y = AddActionRow(content.transform, "RESET SAVE DATA", y, rowH, ResetSaveData);
            y = AddActionRow(content.transform, "SET DEFAULT VALUES", y, rowH, () =>
            {
                SettingsService.ResetToDefaults();
                RefreshAll();
                _status.text = "Defaults restored.";
            });
            contentRt.sizeDelta = new Vector2(0f, y + 12f);

            _status = UiFactory.AddText(box.transform, "Status", string.Empty, 15, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.78f, 0.85f, 0.9f),
                new Vector2(0.08f, 0.02f), new Vector2(0.62f, 0.09f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            var back = UiFactory.AddButton(box.transform, "Back", "BACK",
                new Vector2(0.66f, 0.02f), new Vector2(0.92f, 0.09f), Vector2.zero, Vector2.zero,
                new Color(0.18f, 0.20f, 0.26f, 0.95f), UiFonts.MenuBold);
            back.GetComponentInChildren<Text>().fontSize = 22;
            back.onClick.AddListener(Hide);
        }

        public void Show()
        {
            if (_root != null) _root.SetActive(true);
            gameObject.SetActive(true);
            GameContext.EnsureEventSystem();
            RefreshAll();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void OpenKeyConfig()
        {
            var ctx = GameContext.Instance;
            if (ctx?.InputActions == null)
            {
                _status.text = "Input actions unavailable.";
                return;
            }

            _controls ??= ControlsRebindScreen.EnsurePresent(transform, ctx.InputActions, ctx.Bindings);
            _controls.Show();
            _status.text = "Rebind keys, then press Back.";
        }

        private void ResetSaveData()
        {
            SettingsService.ResetSaveData();
            RefreshAll();
            _status.text = "Save data reset.";
        }

        private void RefreshAll()
        {
            foreach (var row in _rows)
            {
                if (row.CheckboxFill != null)
                    row.CheckboxFill.enabled = row.Key switch
                    {
                        "fullscreen" => SettingsService.Data.fullscreen,
                        "vsync" => SettingsService.Data.vSync,
                        "cursor" => SettingsService.Data.systemCursor,
                        "vfx" => SettingsService.Data.visualEffects,
                        "numbers" => SettingsService.Data.displayNumbers,
                        _ => row.CheckboxFill.enabled
                    };

                if (row.Slider != null)
                {
                    row.Slider.SetValueWithoutNotify(row.Key switch
                    {
                        "brightness" => SettingsService.Data.brightness,
                        "music" => SettingsService.Data.musicVolume,
                        "sfx" => SettingsService.Data.sfxVolume,
                        "shake" => SettingsService.Data.cameraShake,
                        "minimap" => (SettingsService.Data.minimapSize - 0.6f) / 1f,
                        _ => row.Slider.value
                    });
                }

                if (row.ValueText != null)
                {
                    row.ValueText.text = row.Key switch
                    {
                        "resolution" => SettingsService.GetResolutionLabel(),
                        "controller" => SettingsService.ControllerButtonTypes[SettingsService.Data.controllerButtonType],
                        "language" => SettingsService.Languages[SettingsService.Data.languageIndex],
                        _ => row.ValueText.text
                    };
                }
            }
        }

        private float AddHeader(Transform parent, string label, float yTop, float height)
        {
            var row = CreateRow(parent, label, yTop, height);
            UiFactory.AddText(row, "Label", label, 18, TextAnchor.MiddleCenter, new Color(0.88f, 0.88f, 0.92f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UiFonts.MenuBold);
            return yTop + height;
        }

        private float AddCycleRow(Transform parent, string key, string label, float yTop, float height, System.Func<string> getValue, System.Action<int> onCycle)
        {
            var row = CreateRow(parent, key, yTop, height);
            AddLabel(row, label);
            var value = UiFactory.AddText(row.transform, "Value", getValue(), 18, TextAnchor.MiddleRight, Color.white,
                new Vector2(0.52f, 0f), new Vector2(0.82f, 1f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            var prev = UiFactory.AddButton(row.transform, "Prev", "<", new Vector2(0.84f, 0.1f), new Vector2(0.91f, 0.9f),
                Vector2.zero, Vector2.zero, new Color(0.16f, 0.18f, 0.24f, 0.9f), UiFonts.MenuBold);
            prev.GetComponentInChildren<Text>().fontSize = 18;
            prev.onClick.AddListener(() => { onCycle(-1); RefreshAll(); });

            var next = UiFactory.AddButton(row.transform, "Next", ">", new Vector2(0.92f, 0.1f), new Vector2(0.99f, 0.9f),
                Vector2.zero, Vector2.zero, new Color(0.16f, 0.18f, 0.24f, 0.9f), UiFonts.MenuBold);
            next.GetComponentInChildren<Text>().fontSize = 18;
            next.onClick.AddListener(() => { onCycle(1); RefreshAll(); });

            _rows.Add(new RowBinding { Key = key, ValueText = value });
            return yTop + height;
        }

        private float AddToggleRow(Transform parent, string key, string label, float yTop, float height, System.Func<bool> getValue, System.Action toggle)
        {
            var row = CreateRow(parent, key, yTop, height);
            AddLabel(row, label);

            var box = UiFactory.AddPanel(row.transform, "Box", new Color(0.18f, 0.18f, 0.22f, 1f),
                new Vector2(0.90f, 0.18f), new Vector2(0.98f, 0.82f), Vector2.zero, Vector2.zero);
            var fill = UiFactory.AddPanel(box.transform, "Fill", new Color(0.72f, 0.72f, 0.78f, 1f),
                new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), Vector2.zero, Vector2.zero);
            fill.enabled = getValue();

            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box;
            btn.onClick.AddListener(() => { toggle(); RefreshAll(); });

            _rows.Add(new RowBinding { Key = key, CheckboxFill = fill });
            return yTop + height;
        }

        private float AddSliderRow(Transform parent, string key, string label, float yTop, float height, System.Func<float> getValue, System.Action<float> setValue)
        {
            var row = CreateRow(parent, key, yTop, height);
            AddLabel(row, label);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var rt = sliderGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.56f, 0.2f);
            rt.anchorMax = new Vector2(0.98f, 0.8f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var track = UiFactory.AddPanel(sliderGo.transform, "Track", new Color(0.35f, 0.24f, 0.16f, 1f),
                new Vector2(0f, 0.42f), new Vector2(1f, 0.58f), Vector2.zero, Vector2.zero);
            var fill = UiFactory.AddPanel(sliderGo.transform, "Fill", new Color(0.62f, 0.58f, 0.52f, 1f),
                new Vector2(0f, 0.42f), new Vector2(1f, 0.58f), Vector2.zero, Vector2.zero);
            var handle = UiFactory.AddPanel(sliderGo.transform, "Handle", new Color(0.78f, 0.78f, 0.82f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-8f, -10f), new Vector2(8f, 10f));

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = getValue();
            slider.onValueChanged.AddListener(v => setValue(v));

            _rows.Add(new RowBinding { Key = key, Slider = slider });
            return yTop + height;
        }

        private float AddActionRow(Transform parent, string label, float yTop, float height, UnityEngine.Events.UnityAction onClick)
        {
            var row = CreateRow(parent, label, yTop, height);
            var btn = UiFactory.AddButton(row.transform, "Button", label,
                new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.12f, 0.14f, 0.2f), UiFonts.MenuRegular);
            var text = btn.GetComponentInChildren<Text>();
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            btn.onClick.AddListener(onClick);
            return yTop + height;
        }

        private static RectTransform CreateRow(Transform parent, string name, float yTop, float height)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -yTop);
            rt.sizeDelta = new Vector2(0f, height);
            return rt;
        }

        private static void AddLabel(RectTransform row, string label)
        {
            UiFactory.AddText(row.transform, "Label", label, 17, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.02f, 0f), new Vector2(0.52f, 1f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);
        }
    }
}
