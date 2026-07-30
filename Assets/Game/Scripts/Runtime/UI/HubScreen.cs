using RealmShards.Core;
using RealmShards.Input;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Hub: title attract → main menu → lobby with mixed-device join, loadout cycling, route length, controls, start run.
    /// </summary>
    public sealed class HubScreen : MonoBehaviour
    {
        private enum HubState
        {
            Attract,
            Menu,
            Lobby
        }

        private HubState _state;
        private GameObject _titlePanel;
        private GameObject _menuPanel;
        private GameObject _lobbyPanel;
        private Text _pressPrompt;
        private Text _statusText;
        private Text _loadoutText;
        private Text _joinPrompt;
        private Text[] _slotLabels;
        private Button _playButton;
        private int _preCapital = 2;
        private float _promptBlinkTimer;
        private ControlsRebindScreen _controls;
        private LocalCoopLobby _lobby;
        private InputAction _joinAction;
        private InputAction _leaveAction;

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
            Build();
            ShowAttract();
            HookJoinListening(true);
        }

        private void OnDestroy()
        {
            HookJoinListening(false);
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void Update()
        {
            if (_state == HubState.Attract)
            {
                UpdatePressPromptBlink();
                if (WasAnyButtonPressed())
                    ShowMenu();
                return;
            }

            if (_state != HubState.Lobby || _lobbyPanel == null || !_lobbyPanel.activeSelf)
                return;

            // Keyboard Space / Enter join
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                TryJoinDevice(kb, "Keyboard&Mouse");

            // Any gamepad A
            foreach (var pad in Gamepad.all)
            {
                if (pad != null && pad.buttonSouth.wasPressedThisFrame)
                    TryJoinDevice(pad, "Gamepad");
            }

            // Leave: B on claimed pad / Backspace on KBM
            if (kb != null && kb.backspaceKey.wasPressedThisFrame)
            {
                for (int i = 0; i < LocalCoopLobby.MaxPlayers; i++)
                {
                    var s = _lobby.GetSlot(i);
                    if (s.Joined && s.PrimaryDevice is Keyboard)
                    {
                        _lobby.Leave(i);
                        Refresh();
                        break;
                    }
                }
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad == null || !pad.buttonEast.wasPressedThisFrame) continue;
                if (_lobby.Claims.TryGetPlayerForDevice(pad, out int idx))
                {
                    _lobby.Leave(idx);
                    Refresh();
                }
            }
        }

        private void Build()
        {
            var root = transform;

            _titlePanel = new GameObject("TitlePanel", typeof(RectTransform));
            _titlePanel.transform.SetParent(root, false);
            StretchFull(_titlePanel.GetComponent<RectTransform>());

            var titleSprite = Resources.Load<Sprite>("UI/title_screen");
            if (titleSprite != null)
            {
                UiFactory.AddSprite(_titlePanel.transform, "TitleArt", titleSprite,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }
            else
            {
                UiFactory.AddPanel(_titlePanel.transform, "FallbackBackground", new Color(0.08f, 0.09f, 0.12f, 1f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                UiFactory.AddText(_titlePanel.transform, "FallbackTitle", "RealmShards", 64, TextAnchor.MiddleCenter,
                    new Color(0.85f, 0.9f, 1f),
                    new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero);
            }

            _pressPrompt = UiFactory.AddText(_titlePanel.transform, "PressPrompt", "PRESS ANY BUTTON", 24,
                TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.2f, 0.04f), new Vector2(0.8f, 0.10f), Vector2.zero, Vector2.zero);
            _pressPrompt.fontStyle = FontStyle.Bold;

            _menuPanel = new GameObject("MenuPanel", typeof(RectTransform));
            _menuPanel.transform.SetParent(_titlePanel.transform, false);
            StretchFull(_menuPanel.GetComponent<RectTransform>());

            UiFactory.AddPanel(_menuPanel.transform, "MenuDim",
                new Color(0.02f, 0.03f, 0.06f, 0.35f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _playButton = UiFactory.AddButton(_menuPanel.transform, "Play", "Play",
                new Vector2(0.35f, 0.34f), new Vector2(0.65f, 0.44f), Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.45f, 0.32f, 0.95f));
            _playButton.onClick.AddListener(ShowLobby);

            var settings = UiFactory.AddButton(_menuPanel.transform, "Settings", "Settings",
                new Vector2(0.35f, 0.22f), new Vector2(0.65f, 0.32f), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.3f, 0.45f, 0.95f));
            settings.onClick.AddListener(OpenSettings);

            var quit = UiFactory.AddButton(_menuPanel.transform, "Quit", "Quit",
                new Vector2(0.35f, 0.10f), new Vector2(0.65f, 0.20f), Vector2.zero, Vector2.zero,
                new Color(0.45f, 0.18f, 0.18f, 0.95f));
            quit.onClick.AddListener(GameQuit.Request);

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

            _lobbyPanel = new GameObject("LobbyPanel", typeof(RectTransform));
            _lobbyPanel.transform.SetParent(root, false);
            StretchFull(_lobbyPanel.GetComponent<RectTransform>());

            UiFactory.AddPanel(_lobbyPanel.transform, "Background", new Color(0.08f, 0.09f, 0.12f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.AddText(_lobbyPanel.transform, "LobbyTitle", "Hub Lobby", 40, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.9f, 1f),
                new Vector2(0.1f, 0.90f), new Vector2(0.9f, 0.98f), Vector2.zero, Vector2.zero);

            _statusText = UiFactory.AddText(_lobbyPanel.transform, "Status", string.Empty, 18, TextAnchor.UpperLeft, Color.white,
                new Vector2(0.05f, 0.58f), new Vector2(0.48f, 0.88f), Vector2.zero, Vector2.zero);

            _joinPrompt = UiFactory.AddText(_lobbyPanel.transform, "JoinPrompt",
                "Press Space / A to join · Backspace / B to leave",
                18, TextAnchor.MiddleCenter, new Color(0.85f, 0.9f, 0.55f),
                new Vector2(0.5f, 0.84f), new Vector2(0.95f, 0.90f), Vector2.zero, Vector2.zero);

            _slotLabels = new Text[4];
            for (var i = 0; i < 4; i++)
            {
                var yMax = 0.82f - i * 0.08f;
                var yMin = yMax - 0.07f;
                _slotLabels[i] = UiFactory.AddText(_lobbyPanel.transform, $"Slot{i}", $"P{i + 1} — Empty", 20,
                    TextAnchor.MiddleLeft, Color.white,
                    new Vector2(0.52f, yMin), new Vector2(0.95f, yMax), Vector2.zero, Vector2.zero);
            }

            _loadoutText = UiFactory.AddText(_lobbyPanel.transform, "Loadout", string.Empty, 18, TextAnchor.UpperLeft,
                new Color(0.8f, 0.85f, 0.9f),
                new Vector2(0.05f, 0.28f), new Vector2(0.48f, 0.56f), Vector2.zero, Vector2.zero);

            for (int slot = 0; slot < 4; slot++)
            {
                int captured = slot;
                float x0 = 0.05f + slot * 0.11f;
                var cycle = UiFactory.AddButton(_lobbyPanel.transform, $"Cycle{slot}", $"Slot {slot + 1}",
                    new Vector2(x0, 0.20f), new Vector2(x0 + 0.10f, 0.27f), Vector2.zero, Vector2.zero,
                    new Color(0.18f, 0.28f, 0.38f, 1f));
                cycle.GetComponentInChildren<Text>().fontSize = 16;
                cycle.onClick.AddListener(() => CycleLoadoutSlot(captured));
            }

            var nodesBtn = UiFactory.AddButton(_lobbyPanel.transform, "Nodes", "Cities before Capital: 2",
                new Vector2(0.52f, 0.22f), new Vector2(0.95f, 0.30f), Vector2.zero, Vector2.zero,
                new Color(0.22f, 0.28f, 0.36f, 1f));
            nodesBtn.GetComponentInChildren<Text>().fontSize = 18;
            nodesBtn.onClick.AddListener(() =>
            {
                _preCapital = _preCapital >= 3 ? 1 : _preCapital + 1;
                nodesBtn.GetComponentInChildren<Text>().text = $"Cities before Capital: {_preCapital}";
                if (GameContext.Instance != null)
                    GameContext.Instance.Save.Current.meta.preferredPreCapitalNodes = _preCapital;
            });

            var startRun = UiFactory.AddButton(_lobbyPanel.transform, "StartRun", "Start Run",
                new Vector2(0.55f, 0.06f), new Vector2(0.95f, 0.16f), Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.45f, 0.32f, 1f));
            startRun.onClick.AddListener(OnStartRun);

            var controls = UiFactory.AddButton(_lobbyPanel.transform, "Controls", "Controls",
                new Vector2(0.28f, 0.06f), new Vector2(0.52f, 0.14f), Vector2.zero, Vector2.zero);
            controls.onClick.AddListener(OpenControls);

            var saveBtn = UiFactory.AddButton(_lobbyPanel.transform, "SaveNow", "Save",
                new Vector2(0.05f, 0.06f), new Vector2(0.25f, 0.14f), Vector2.zero, Vector2.zero);
            saveBtn.onClick.AddListener(() =>
            {
                GameContext.Instance?.Save.Save();
                Refresh();
            });

            if (GameContext.Instance != null)
            {
                _preCapital = Mathf.Clamp(GameContext.Instance.Save.Current.meta.preferredPreCapitalNodes, 1, 3);
                nodesBtn.GetComponentInChildren<Text>().text = $"Cities before Capital: {_preCapital}";
            }
        }

        private void OpenSettings()
        {
            var ctx = GameContext.Instance;
            if (ctx == null) return;
            _controls ??= ControlsRebindScreen.EnsurePresent(transform, ctx.InputActions, ctx.Bindings);
            _controls.Show();
        }

        private void OpenControls()
        {
            OpenSettings();
        }

        private void TryJoinDevice(InputDevice device, string scheme)
        {
            if (_lobby.TryJoin(device, scheme, out _, out var fail))
            {
                Refresh();
            }
            else if (!string.IsNullOrEmpty(fail) && fail != "Device already in use.")
            {
                if (_joinPrompt != null)
                    _joinPrompt.text = fail;
            }
        }

        private void CycleLoadoutSlot(int slot)
        {
            var ctx = GameContext.Instance;
            if (ctx == null) return;
            var unlocked = ctx.Save.Current.meta.unlockedAbilityIds;
            if (unlocked == null) return;

            var equipped = ctx.Save.Current.meta.equippedAbilityIds;
            while (equipped.Count < 4) equipped.Add(string.Empty);

            // Cycle: (empty) → each unlocked spell → back to empty.
            var options = new System.Collections.Generic.List<string>(unlocked.Count + 1) { string.Empty };
            for (int i = 0; i < unlocked.Count; i++)
            {
                if (string.IsNullOrEmpty(unlocked[i])) continue;
                if (!options.Contains(unlocked[i]))
                    options.Add(unlocked[i]);
            }

            string current = equipped[slot] ?? string.Empty;
            // Drop unequipped/locked leftovers so cycling never sticks on Arcane Bolt.
            if (!string.IsNullOrEmpty(current) && !options.Contains(current))
                current = string.Empty;

            int idx = options.IndexOf(current);
            if (idx < 0) idx = 0;
            idx = (idx + 1) % options.Count;
            ctx.Progression.SetEquippedAbility(slot, options[idx], saveImmediately: true);
            Refresh();
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
            _titlePanel.SetActive(true);
            _menuPanel.SetActive(false);
            _lobbyPanel.SetActive(false);
            if (_pressPrompt != null)
            {
                _pressPrompt.gameObject.SetActive(true);
                _promptBlinkTimer = 0f;
            }
        }

        private void ShowMenu()
        {
            _state = HubState.Menu;
            _titlePanel.SetActive(true);
            _menuPanel.SetActive(true);
            _lobbyPanel.SetActive(false);
            if (_pressPrompt != null)
                _pressPrompt.gameObject.SetActive(false);
            GameContext.EnsureEventSystem();
            if (_playButton != null)
                _playButton.Select();
        }

        private void ShowLobby()
        {
            _state = HubState.Lobby;
            _titlePanel.SetActive(false);
            _lobbyPanel.SetActive(true);
            Refresh();
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

        private void Refresh()
        {
            var ctx = GameContext.Instance;
            if (ctx == null || _statusText == null) return;

            var meta = ctx.Save.Current.meta;
            _statusText.text =
                $"Year: {meta.year}   Decade: {meta.decade}\n" +
                $"Arcane Vestiges: {meta.arcaneVestiges}\n" +
                $"Players joined: {_lobby.JoinedCount}/4\n" +
                $"Save: {ctx.Save.SaveFilePath}";

            var loadout = "Loadout (unlocked spells)\n";
            for (var i = 0; i < meta.equippedAbilityIds.Count && i < 4; i++)
            {
                var id = meta.equippedAbilityIds[i];
                var label = string.IsNullOrEmpty(id) ? "(empty)" : ctx.Content.GetDisplayName(id, id);
                loadout += $"  [{i + 1}] {label}\n";
            }

            loadout += "\nTap Slot buttons to cycle (includes Empty).";
            _loadoutText.text = loadout;

            Color[] colors =
            {
                new Color(0.72f, 0.45f, 0.95f),
                new Color(0.35f, 0.82f, 0.42f),
                new Color(0.95f, 0.82f, 0.28f),
                new Color(0.92f, 0.28f, 0.28f)
            };

            for (var i = 0; i < _slotLabels.Length; i++)
            {
                var slot = _lobby.GetSlot(i);
                _slotLabels[i].color = colors[i];
                _slotLabels[i].text = slot.Joined
                    ? $"P{i + 1} — Joined ({slot.DeviceLabel})"
                    : $"P{i + 1} — Empty";
            }

            if (_joinPrompt != null)
                _joinPrompt.text = "Press Space / A to join · Backspace / B to leave";
        }

        private void OnStartRun()
        {
            var ctx = GameContext.Instance;
            if (ctx == null) return;

            int count = Mathf.Max(1, _lobby.JoinedCount);
            // If nobody joined yet, auto-join keyboard as P1 for convenience.
            if (_lobby.JoinedCount == 0 && Keyboard.current != null)
                _lobby.TryJoin(Keyboard.current, "Keyboard&Mouse", out _, out _);

            count = Mathf.Max(1, _lobby.JoinedCount);
            ctx.Save.Current.settings.localPlayerCount = count;
            ctx.Runs.BeginWorldRun(_preCapital, count);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                _lobby?.HandleDeviceLost(device);
                Refresh();
            }
        }

        private void HookJoinListening(bool enabled)
        {
            _ = enabled;
            _ = _joinAction;
            _ = _leaveAction;
        }
    }
}
