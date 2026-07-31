using RealmShards.Save;
using RealmShards.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.Core
{
    /// <summary>
    /// Applies persisted settings to display, audio, and gameplay systems.
    /// </summary>
    public static class SettingsService
    {
        public static readonly (int width, int height, string label)[] Resolutions =
        {
            (1280, 720, "1280 x 720"),
            (1366, 768, "1366 x 768"),
            (1600, 900, "1600 x 900"),
            (1920, 1080, "1920 x 1080"),
            (2560, 1440, "2560 x 1440")
        };

        public static readonly string[] Languages = { "English" };
        public static readonly string[] ControllerButtonTypes = { "Type 1", "Type 2" };

        private static ISaveService _save;
        private static BrightnessOverlay _brightnessOverlay;

        public static SettingsData Data => _save?.Current.settings;

        public static bool VisualEffectsEnabled => Data == null || Data.visualEffects;
        public static bool DisplayNumbersEnabled => Data == null || Data.displayNumbers;
        public static float CameraShakeStrength => Data?.cameraShake ?? 1f;
        public static float MinimapSizeScale => Mathf.Clamp(Data?.minimapSize ?? 1f, 0.6f, 1.6f);
        public static float MasterVolume => Mathf.Clamp01(Data?.masterVolume ?? 1f);
        public static float MusicVolume => Mathf.Clamp01(Data?.musicVolume ?? 0.8f);
        public static float SfxVolume => Mathf.Clamp01(Data?.sfxVolume ?? 1f);

        public static void Initialize(ISaveService save)
        {
            _save = save;
            ClampStoredValues();
            ApplyAll();
        }

        public static void Save()
        {
            _save?.Save();
        }

        public static void ApplyAll()
        {
            if (Data == null) return;
            ClampStoredValues();
            ApplyResolution();
            ApplyVSync();
            ApplyCursor();
            ApplyBrightness();
            ApplyAudio();
        }

        public static void ResetToDefaults()
        {
            if (Data == null) return;
            var players = Data.localPlayerCount;
            var defaults = new SettingsData { localPlayerCount = players };
            defaults.resolutionIndex = FindClosestResolutionIndex(Screen.width, Screen.height);
            CopySettings(defaults, Data);
            ApplyAll();
            Save();
        }

        public static string GetResolutionLabel()
        {
            var idx = Mathf.Clamp(Data?.resolutionIndex ?? 0, 0, Resolutions.Length - 1);
            return Resolutions[idx].label;
        }

        public static void CycleResolution(int delta)
        {
            if (Data == null) return;
            Data.resolutionIndex = (Data.resolutionIndex + delta + Resolutions.Length) % Resolutions.Length;
            ApplyResolution();
            Save();
        }

        public static void ToggleFullscreen()
        {
            if (Data == null) return;
            Data.fullscreen = !Data.fullscreen;
            ApplyResolution();
            Save();
        }

        public static void ToggleVSync()
        {
            if (Data == null) return;
            Data.vSync = !Data.vSync;
            ApplyVSync();
            Save();
        }

        public static void ToggleSystemCursor()
        {
            if (Data == null) return;
            Data.systemCursor = !Data.systemCursor;
            ApplyCursor();
            Save();
        }

        public static void ToggleVisualEffects()
        {
            if (Data == null) return;
            Data.visualEffects = !Data.visualEffects;
            Save();
        }

        public static void ToggleDisplayNumbers()
        {
            if (Data == null) return;
            Data.displayNumbers = !Data.displayNumbers;
            Save();
        }

        public static void SetBrightness(float value)
        {
            if (Data == null) return;
            Data.brightness = Mathf.Clamp01(value);
            ApplyBrightness();
            Save();
        }

        public static void SetMasterVolume(float value)
        {
            if (Data == null) return;
            Data.masterVolume = Mathf.Clamp01(value);
            ApplyAudio();
            Save();
        }

        public static void SetMusicVolume(float value)
        {
            if (Data == null) return;
            Data.musicVolume = Mathf.Clamp01(value);
            ApplyAudio();
            Save();
        }

        public static void SetSfxVolume(float value)
        {
            if (Data == null) return;
            Data.sfxVolume = Mathf.Clamp01(value);
            ApplyAudio();
            Save();
        }

        public static void SetCameraShake(float value)
        {
            if (Data == null) return;
            Data.cameraShake = Mathf.Clamp01(value);
            Save();
        }

        public static void SetMinimapSize(float value)
        {
            if (Data == null) return;
            Data.minimapSize = Mathf.Clamp(value, 0.6f, 1.6f);
            Save();
        }

        public static void CycleLanguage(int delta)
        {
            if (Data == null) return;
            Data.languageIndex = (Data.languageIndex + delta + Languages.Length) % Languages.Length;
            Save();
        }

        public static void CycleControllerButtons(int delta)
        {
            if (Data == null) return;
            Data.controllerButtonType = (Data.controllerButtonType + delta + ControllerButtonTypes.Length) % ControllerButtonTypes.Length;
            Save();
        }

        public static void ResetSaveData()
        {
            _save?.DeleteSave();
            _save?.LoadOrCreate();
            ClampStoredValues();
            ApplyAll();
        }

        private static void ApplyResolution()
        {
            if (Data == null) return;
            var idx = Mathf.Clamp(Data.resolutionIndex, 0, Resolutions.Length - 1);
            var res = Resolutions[idx];
            var mode = Data.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            Screen.SetResolution(res.width, res.height, mode);
        }

        private static void ApplyVSync()
        {
            QualitySettings.vSyncCount = Data != null && Data.vSync ? 1 : 0;
        }

        private static void ApplyCursor()
        {
            if (Data == null) return;
            Cursor.visible = Data.systemCursor;
            Cursor.lockState = Data.systemCursor ? CursorLockMode.None : CursorLockMode.Confined;
        }

        private static void ApplyBrightness()
        {
            if (Data == null) return;
            EnsureBrightnessOverlay();
            _brightnessOverlay.SetBrightness(Data.brightness);
        }

        private static void ApplyAudio()
        {
            AudioListener.volume = MasterVolume;
        }

        private static void EnsureBrightnessOverlay()
        {
            if (_brightnessOverlay != null) return;
            var go = new GameObject(nameof(BrightnessOverlay));
            Object.DontDestroyOnLoad(go);
            _brightnessOverlay = go.AddComponent<BrightnessOverlay>();
        }

        private static void ClampStoredValues()
        {
            if (Data == null) return;
            Data.resolutionIndex = Mathf.Clamp(Data.resolutionIndex, 0, Resolutions.Length - 1);
            Data.brightness = Mathf.Clamp01(Data.brightness);
            Data.masterVolume = Mathf.Clamp01(Data.masterVolume);
            Data.musicVolume = Mathf.Clamp01(Data.musicVolume);
            Data.sfxVolume = Mathf.Clamp01(Data.sfxVolume);
            Data.cameraShake = Mathf.Clamp01(Data.cameraShake);
            Data.minimapSize = Mathf.Clamp(Data.minimapSize, 0.6f, 1.6f);
            if (Data.languageIndex < 0 || Data.languageIndex >= Languages.Length)
                Data.languageIndex = 0;
            if (Data.controllerButtonType < 0 || Data.controllerButtonType >= ControllerButtonTypes.Length)
                Data.controllerButtonType = 0;
        }

        private static int FindClosestResolutionIndex(int width, int height)
        {
            int best = 0;
            long bestDiff = long.MaxValue;
            for (int i = 0; i < Resolutions.Length; i++)
            {
                long diff = Mathf.Abs(Resolutions[i].width - width) + Mathf.Abs(Resolutions[i].height - height);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = i;
                }
            }

            return best;
        }

        private static void CopySettings(SettingsData source, SettingsData target)
        {
            target.masterVolume = source.masterVolume;
            target.musicVolume = source.musicVolume;
            target.sfxVolume = source.sfxVolume;
            target.resolutionIndex = source.resolutionIndex;
            target.fullscreen = source.fullscreen;
            target.brightness = source.brightness;
            target.vSync = source.vSync;
            target.systemCursor = source.systemCursor;
            target.visualEffects = source.visualEffects;
            target.cameraShake = source.cameraShake;
            target.minimapSize = source.minimapSize;
            target.displayNumbers = source.displayNumbers;
            target.controllerButtonType = source.controllerButtonType;
            target.languageIndex = source.languageIndex;
        }

        private sealed class BrightnessOverlay : MonoBehaviour
        {
            private Image _image;

            private void Awake()
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;
                canvas.pixelPerfect = false;
                var scaler = gameObject.AddComponent<CanvasScaler>();
                UiScaleConfig.Apply(scaler);

                var panel = new GameObject("Dim", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(transform, false);
                var rt = panel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                _image = panel.GetComponent<Image>();
                _image.color = Color.black;
                _image.raycastTarget = false;
            }

            public void SetBrightness(float brightness)
            {
                if (_image == null) return;
                var alpha = Mathf.Clamp01(1f - brightness) * 0.75f;
                var c = _image.color;
                c.a = alpha;
                _image.color = c;
            }
        }
    }
}
