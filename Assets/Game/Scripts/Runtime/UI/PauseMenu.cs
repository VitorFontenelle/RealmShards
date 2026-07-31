using RealmShards.Combat;
using RealmShards.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Pause overlay: Resume / Controls / Quit to Hub. Restores timeScale on leave.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private bool _paused;
        private GameObject _overlay;
        private Button _resumeButton;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<PauseMenu>() != null)
                return;
            var go = new GameObject(nameof(PauseMenu));
            go.AddComponent<PauseMenu>();
        }

        private void OnEnable() => SceneManager.sceneUnloaded += OnSceneUnloaded;
        private void OnDisable() => SceneManager.sceneUnloaded -= OnSceneUnloaded;

        private void OnDestroy() => HitStop.EnsureRunningTimeScale();

        private void OnSceneUnloaded(Scene _) => HitStop.EnsureRunningTimeScale();

        private void Update()
        {
            if (WasPausePressed())
                Toggle();
        }

        private static bool WasPausePressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
                return true;

            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                if (pads[i].startButton.wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        public void Toggle()
        {
            if (_paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (_paused) return;
            _paused = true;
            HitStop.SetMenuPaused(true);
            Audio.AudioEventHub.Play("ui.pause");
            EnsureOverlay();
            _overlay.SetActive(true);
            GameContext.EnsureEventSystem();
            if (_resumeButton != null)
                _resumeButton.Select();
        }

        public void Resume()
        {
            if (!_paused) return;
            _paused = false;
            HitStop.SetMenuPaused(false);
            Audio.AudioEventHub.Play("ui.resume");
            if (_overlay != null)
                _overlay.SetActive(false);
        }

        private void EnsureOverlay()
        {
            if (_overlay != null) return;

            var canvas = UiFactory.CreateScreenCanvas("PauseMenu", 400);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            canvas.transform.SetParent(transform, false);
            _overlay = canvas.gameObject;

            UiFactory.AddPanel(canvas.transform, "Dim",
                new Color(0.02f, 0.03f, 0.05f, 0.72f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.AddText(canvas.transform, "Title", "Paused", 42,
                TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.3f, 0.72f), new Vector2(0.7f, 0.86f), Vector2.zero, Vector2.zero);

            UiFactory.AddText(canvas.transform, "Hint", "Esc / Start · gamepad navigate", 16,
                TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f),
                new Vector2(0.25f, 0.66f), new Vector2(0.75f, 0.72f), Vector2.zero, Vector2.zero);

            _resumeButton = UiFactory.AddButton(canvas.transform, "Resume", "Resume",
                new Vector2(0.35f, 0.52f), new Vector2(0.65f, 0.62f), Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.4f, 0.3f, 0.95f));
            _resumeButton.onClick.AddListener(Resume);

            var settings = UiFactory.AddButton(canvas.transform, "Settings", "Settings",
                new Vector2(0.35f, 0.40f), new Vector2(0.65f, 0.50f), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.3f, 0.45f, 0.95f));
            settings.onClick.AddListener(OpenSettings);

            var quit = UiFactory.AddButton(canvas.transform, "Quit", "Quit to Hub",
                new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.38f), Vector2.zero, Vector2.zero,
                new Color(0.45f, 0.18f, 0.18f, 0.95f));
            quit.onClick.AddListener(QuitToHub);

            // Explicit navigation for gamepads
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnDown = settings;
            _resumeButton.navigation = nav;
            nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = _resumeButton;
            nav.selectOnDown = quit;
            settings.navigation = nav;
            nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = settings;
            quit.navigation = nav;
        }

        private void OpenSettings()
        {
            OptionsScreen.EnsurePresent(transform).Show();
        }

        private void OpenControls()
        {
            OpenSettings();
        }

        private void QuitToHub()
        {
            HitStop.EnsureRunningTimeScale();
            _paused = false;
            var ctx = GameContext.Instance;
            if (ctx?.RunSession != null && ctx.RunSession.IsActive)
            {
                ctx.Runs.EndRun(Runs.RunOutcome.Failure(
                    ctx.RunSession.CityId ?? Save.ContentIdDefaults.CityStarter,
                    ctx.RunSession.RouteId ?? Save.ContentIdDefaults.RouteWorldMain,
                    "Abandoned run — returned to Hub."));
                return;
            }

            SceneManager.LoadScene(SceneNames.Hub);
        }
    }
}
