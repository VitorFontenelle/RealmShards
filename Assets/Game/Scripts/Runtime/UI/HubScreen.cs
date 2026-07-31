using System.Collections;
using RealmShards.Core;
using RealmShards.Input;
using RealmShards.Save;
using RealmShards.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Hub: title attract → main menu → visual tile lobby.
    /// </summary>
    public sealed class HubScreen : MonoBehaviour
    {
        private enum HubState
        {
            Attract,
            Transitioning,
            Menu,
            Lobby
        }

        private const float TransitionDuration = 0.95f;
        private static readonly Color MenuTextNormal = new(0.94f, 0.92f, 0.98f, 1f);
        private static readonly Color MenuTextHighlight = new(0.78f, 0.55f, 1f, 1f);
        private static readonly Color FlashColor = new(0.45f, 0.18f, 0.82f, 1f);

        private HubState _state;
        private GameObject _attractPanel;
        private GameObject _menuPanel;
        private CanvasGroup _attractGroup;
        private CanvasGroup _menuGroup;
        private CanvasGroup _menuButtonsGroup;
        private CanvasGroup _pressPromptGroup;
        private Image _transitionFlash;
        private Text _pressPrompt;
        private Button _playButton;
        private RectTransform[] _menuButtonRects;
        private int _preCapital = 2;
        private float _promptBlinkTimer;
        private Coroutine _transitionRoutine;
        private LocalCoopLobby _lobby;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<HubScreen>() != null)
                return;

            var canvas = UiFactory.CreateScreenCanvas("HubUI");
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            canvas.gameObject.AddComponent<HubScreen>();
        }

        private void Start()
        {
            _lobby = GameContext.Instance != null
                ? GameContext.Instance.Lobby
                : new LocalCoopLobby();
            _lobby.ResetAll();

            if (GameContext.Instance != null)
                _preCapital = Mathf.Clamp(GameContext.Instance.Save.Current.meta.preferredPreCapitalNodes, 1, 3);

            Build();
            ShowAttract();
        }

        private void Update()
        {
            if (_state == HubState.Attract)
            {
                UpdatePressPromptBlink();
                if (WasAnyButtonPressed())
                    BeginMenuTransition();
            }
        }

        private void Build()
        {
            var root = transform;

            // Full-bleed attract art (no SafeArea inset — that caused the blue camera border).
            _attractPanel = new GameObject("AttractPanel", typeof(RectTransform));
            _attractPanel.transform.SetParent(root, false);
            StretchFull(_attractPanel.GetComponent<RectTransform>());
            _attractGroup = UiFactory.AddCanvasGroup(_attractPanel);

            var titleSprite = Resources.Load<Sprite>("UI/title_screen");
            if (titleSprite != null)
            {
                var art = UiFactory.AddSprite(_attractPanel.transform, "TitleArt", titleSprite,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                art.preserveAspect = false;
                art.type = Image.Type.Simple;
            }
            else
            {
                UiFactory.AddPanel(_attractPanel.transform, "FallbackBackground", new Color(0.08f, 0.09f, 0.12f, 1f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UiFactory.AddText(_attractPanel.transform, "FallbackTitle", "REALMSHARDS", 64, TextAnchor.MiddleCenter,
                    new Color(0.85f, 0.9f, 1f),
                    new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero, UiFonts.MenuBold);
            }

            var pressGo = new GameObject("PressPrompt", typeof(RectTransform));
            pressGo.transform.SetParent(_attractPanel.transform, false);
            StretchFull(pressGo.GetComponent<RectTransform>());
            _pressPromptGroup = UiFactory.AddCanvasGroup(pressGo);

            _pressPrompt = UiFactory.AddText(pressGo.transform, "Label", "PRESS ANY BUTTON", 26,
                TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.10f), Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            var promptOutline = _pressPrompt.gameObject.AddComponent<Outline>();
            promptOutline.effectColor = new Color(0.55f, 0.25f, 0.95f, 0.45f);
            promptOutline.effectDistance = new Vector2(1.5f, -1.5f);

            _menuPanel = new GameObject("MenuPanel", typeof(RectTransform));
            _menuPanel.transform.SetParent(root, false);
            StretchFull(_menuPanel.GetComponent<RectTransform>());
            _menuGroup = UiFactory.AddCanvasGroup(_menuPanel, 0f, false, false);

            var menuSprite = Resources.Load<Sprite>("UI/menu_background");
            if (menuSprite != null)
            {
                UiFactory.AddSprite(_menuPanel.transform, "MenuArt", menuSprite,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                UiFactory.AddPanel(_menuPanel.transform, "FallbackBackground", new Color(0.05f, 0.04f, 0.10f, 1f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            var buttonsRoot = new GameObject("MenuButtons", typeof(RectTransform));
            buttonsRoot.transform.SetParent(_menuPanel.transform, false);
            StretchFull(buttonsRoot.GetComponent<RectTransform>());
            _menuButtonsGroup = UiFactory.AddCanvasGroup(buttonsRoot, 0f, false, false);

            _playButton = UiFactory.AddMenuTextButton(buttonsRoot.transform, "Play", "Play",
                new Vector2(0.34f, 0.44f), new Vector2(0.66f, 0.54f), MenuTextNormal, MenuTextHighlight);
            _playButton.onClick.AddListener(ShowLobby);

            var settings = UiFactory.AddMenuTextButton(buttonsRoot.transform, "Settings", "Settings",
                new Vector2(0.34f, 0.34f), new Vector2(0.66f, 0.44f), MenuTextNormal, MenuTextHighlight);
            settings.onClick.AddListener(OpenSettings);

            var quit = UiFactory.AddMenuTextButton(buttonsRoot.transform, "Quit", "Quit",
                new Vector2(0.34f, 0.24f), new Vector2(0.66f, 0.34f), MenuTextNormal, MenuTextHighlight);
            quit.onClick.AddListener(GameQuit.Request);

            _menuButtonRects = new[]
            {
                _playButton.GetComponent<RectTransform>(),
                settings.GetComponent<RectTransform>(),
                quit.GetComponent<RectTransform>()
            };

            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnDown = settings;
            _playButton.navigation = nav;
            nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = _playButton;
            nav.selectOnDown = quit;
            settings.navigation = nav;
            nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnUp = settings;
            quit.navigation = nav;

            var flashGo = new GameObject("TransitionFlash", typeof(RectTransform), typeof(Image));
            flashGo.transform.SetParent(root, false);
            StretchFull(flashGo.GetComponent<RectTransform>());
            _transitionFlash = flashGo.GetComponent<Image>();
            _transitionFlash.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f);
            _transitionFlash.raycastTarget = false;
            flashGo.SetActive(false);
        }

        private void BeginMenuTransition()
        {
            if (_state == HubState.Transitioning || _state == HubState.Menu)
                return;

            _state = HubState.Transitioning;
            if (_transitionRoutine != null)
                StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(PlayMenuTransition());
        }

        private IEnumerator PlayMenuTransition()
        {
            _menuPanel.SetActive(true);
            _menuGroup.alpha = 0f;
            _menuGroup.interactable = false;
            _menuGroup.blocksRaycasts = false;
            _menuButtonsGroup.alpha = 0f;
            _menuButtonsGroup.interactable = false;
            _menuButtonsGroup.blocksRaycasts = false;

            PrepareMenuButtonsForEntrance();

            _transitionFlash.gameObject.SetActive(true);
            _transitionFlash.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f);

            var elapsed = 0f;
            while (elapsed < TransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / TransitionDuration);

                _pressPromptGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(t / 0.18f));
                _attractGroup.alpha = Mathf.Lerp(1f, 0f, EaseInOutCubic(Mathf.Clamp01((t - 0.05f) / 0.55f)));
                _menuGroup.alpha = Mathf.Lerp(0f, 1f, EaseInOutCubic(Mathf.Clamp01((t - 0.08f) / 0.62f)));

                var flashT = t < 0.18f
                    ? t / 0.18f
                    : 1f - Mathf.Clamp01((t - 0.18f) / 0.42f);
                _transitionFlash.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, flashT * 0.72f);

                AnimateMenuButtons(t);
                yield return null;
            }

            _transitionFlash.gameObject.SetActive(false);
            _attractPanel.SetActive(false);
            _attractGroup.alpha = 1f;
            _pressPromptGroup.alpha = 1f;
            _menuGroup.alpha = 1f;
            _menuGroup.interactable = true;
            _menuGroup.blocksRaycasts = true;
            _menuButtonsGroup.alpha = 1f;
            _menuButtonsGroup.interactable = true;
            _menuButtonsGroup.blocksRaycasts = true;
            FinalizeMenuButtons();

            _state = HubState.Menu;
            GameContext.EnsureEventSystem();
            if (_playButton != null)
                _playButton.Select();

            _transitionRoutine = null;
        }

        private void PrepareMenuButtonsForEntrance()
        {
            if (_menuButtonRects == null) return;
            for (int i = 0; i < _menuButtonRects.Length; i++)
            {
                var rt = _menuButtonRects[i];
                if (rt == null) continue;
                rt.localScale = Vector3.one * 0.92f;
                var pos = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(pos.x, pos.y - 28f);
            }
        }

        private void AnimateMenuButtons(float transitionT)
        {
            if (_menuButtonRects == null) return;
            for (int i = 0; i < _menuButtonRects.Length; i++)
            {
                var rt = _menuButtonRects[i];
                if (rt == null) continue;

                var start = 0.28f + i * 0.1f;
                var local = Mathf.Clamp01((transitionT - start) / 0.34f);
                var eased = EaseOutBack(local);
                rt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
                var y = Mathf.Lerp(-28f, 0f, eased);
                var pos = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(pos.x, y);
            }

            var buttonFadeStart = 0.24f;
            var buttonFade = Mathf.Clamp01((transitionT - buttonFadeStart) / 0.42f);
            _menuButtonsGroup.alpha = EaseOutCubic(buttonFade);
        }

        private void FinalizeMenuButtons()
        {
            if (_menuButtonRects == null) return;
            for (int i = 0; i < _menuButtonRects.Length; i++)
            {
                var rt = _menuButtonRects[i];
                if (rt == null) continue;
                rt.localScale = Vector3.one;
                var pos = rt.anchoredPosition;
                rt.anchoredPosition = new Vector2(pos.x, 0f);
            }
        }

        private static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void OpenSettings()
        {
            OptionsScreen.EnsurePresent(transform).Show();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ShowAttract()
        {
            _state = HubState.Attract;
            _attractPanel.SetActive(true);
            _attractGroup.alpha = 1f;
            _menuPanel.SetActive(false);
            _menuGroup.alpha = 0f;
            _menuButtonsGroup.alpha = 0f;
            if (_pressPromptGroup != null)
            {
                _pressPromptGroup.alpha = 1f;
                _promptBlinkTimer = 0f;
            }
        }

        private void ShowLobby()
        {
            _state = HubState.Lobby;
            _attractPanel.SetActive(false);
            _menuPanel.SetActive(false);

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
                canvas.enabled = false;

            var world = HubLobbyWorld.EnsurePresent();
            world.SetPreCapital(_preCapital);
            world.Show();
        }

        private void UpdatePressPromptBlink()
        {
            if (_pressPrompt == null) return;
            _promptBlinkTimer += Time.unscaledDeltaTime;
            var alpha = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(_promptBlinkTimer * 4f));
            var color = _pressPrompt.color;
            color.a = alpha;
            _pressPrompt.color = color;
        }

        private static bool WasAnyButtonPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame)
                return true;

            var mouse = Mouse.current;
            if (mouse != null && (mouse.leftButton.wasPressedThisFrame
                                  || mouse.rightButton.wasPressedThisFrame
                                  || mouse.middleButton.wasPressedThisFrame))
                return true;

            var pads = Gamepad.all;
            for (int i = 0; i < pads.Count; i++)
            {
                var pad = pads[i];
                if (pad == null) continue;
                if (pad.buttonSouth.wasPressedThisFrame
                    || pad.buttonEast.wasPressedThisFrame
                    || pad.buttonWest.wasPressedThisFrame
                    || pad.buttonNorth.wasPressedThisFrame
                    || pad.startButton.wasPressedThisFrame
                    || pad.selectButton.wasPressedThisFrame)
                    return true;
            }

            return false;
        }
    }
}
