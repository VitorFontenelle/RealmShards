using System.Collections.Generic;
using RealmShards.CameraSystem;
using RealmShards.Core;
using RealmShards.Input;
using RealmShards.Progression;
using RealmShards.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards.World
{
    /// <summary>
    /// Visual hub lobby: tiled room, join avatars, tome loadout selection, northern exit to start run.
    /// </summary>
    public sealed class HubLobbyWorld : MonoBehaviour
    {
        private HubLobbyArena.LobbyArenaResult _arena;
        private HubLobbyJoinHud _joinHud;
        private TomePedestal _tome;
        private TomeSpellSelectScreen _spellUi;
        private LocalCoopLobby _lobby;
        private readonly GameObject[] _avatars = new GameObject[LocalCoopLobby.MaxPlayers];
        private readonly PlayerOverheadHealthBar[] _healthBars = new PlayerOverheadHealthBar[LocalCoopLobby.MaxPlayers];
        private BoxCollider2D _exitCollider;
        private int _preCapital = 2;
        private bool _startingRun;

        public static HubLobbyWorld EnsurePresent()
        {
            var existing = FindFirstObjectByType<HubLobbyWorld>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(HubLobbyWorld));
            return go.AddComponent<HubLobbyWorld>();
        }

        private void Awake()
        {
            _lobby = GameContext.Instance != null ? GameContext.Instance.Lobby : new LocalCoopLobby();
            _preCapital = Mathf.Clamp(GameContext.Instance?.Save?.Current?.meta?.preferredPreCapitalNodes ?? 2, 1, 3);
            BuildWorld();
            _joinHud = HubLobbyJoinHud.EnsurePresent();
            _spellUi = TomeSpellSelectScreen.EnsurePresent(transform);
            RefreshJoinHud();
        }

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void Update()
        {
            PollJoinInput();
            PollTomeInteract();
            PollExit();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _joinHud?.Show();
            RefreshJoinHud();
        }

        public void Hide()
        {
            _joinHud?.Hide();
            _spellUi?.Hide();
            gameObject.SetActive(false);
        }

        public void SetPreCapital(int value) => _preCapital = Mathf.Clamp(value, 1, 3);

        private void BuildWorld()
        {
            _arena = HubLobbyArena.Build(transform);
            SetupCamera();
            _tome = TomePedestal.Create(_arena.Root, _arena.TomeSpawn.position);
            _exitCollider = _arena.ExitTrigger.GetComponent<BoxCollider2D>();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                cam.orthographic = true;
                cam.orthographicSize = 7.5f;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }
            else
            {
                cam.orthographic = true;
                cam.orthographicSize = 7.5f;
                cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }

            var shared = cam.GetComponent<SharedOrthoCamera>();
            if (shared == null)
                shared = cam.gameObject.AddComponent<SharedOrthoCamera>();
            shared.Configure(_arena.Bounds, _arena.Root, 5f, 12f);
        }

        private void PollJoinInput()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                TryJoinDevice(kb, "Keyboard&Mouse");

            foreach (var pad in Gamepad.all)
            {
                if (pad != null && pad.buttonSouth.wasPressedThisFrame)
                    TryJoinDevice(pad, "Gamepad");
            }

            if (kb != null && kb.backspaceKey.wasPressedThisFrame)
            {
                for (int i = 0; i < LocalCoopLobby.MaxPlayers; i++)
                {
                    var s = _lobby.GetSlot(i);
                    if (s.Joined && s.PrimaryDevice is Keyboard)
                    {
                        LeavePlayer(i);
                        break;
                    }
                }
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad == null || !pad.buttonEast.wasPressedThisFrame) continue;
                if (_lobby.Claims.TryGetPlayerForDevice(pad, out int idx))
                    LeavePlayer(idx);
            }
        }

        private void TryJoinDevice(InputDevice device, string scheme)
        {
            if (_lobby.TryJoin(device, scheme, out int playerIndex, out _))
            {
                SpawnAvatar(playerIndex);
                RefreshJoinHud();
            }
        }

        private void LeavePlayer(int playerIndex)
        {
            _lobby.Leave(playerIndex);
            if (_avatars[playerIndex] != null)
            {
                Destroy(_avatars[playerIndex]);
                _avatars[playerIndex] = null;
            }

            _healthBars[playerIndex] = null;
            RefreshJoinHud();
        }

        private void SpawnAvatar(int playerIndex)
        {
            if (_avatars[playerIndex] != null)
                return;

            GameObject prefab = RuntimeContentCatalog.Get()?.PlayerPrefab;
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CityRunBootstrap.PlayerPrefabPath);
#endif
            Vector3 pos = _arena.PlayerSpawns[Mathf.Clamp(playerIndex, 0, _arena.PlayerSpawns.Length - 1)];
            GameObject instance;
            if (prefab != null)
            {
                instance = Instantiate(prefab, pos, Quaternion.identity, _arena.Root);
                instance.name = $"LobbyPlayer_{playerIndex + 1}";
                try { instance.tag = "Player"; } catch { }
                instance.GetComponent<PlayerController>()?.InitializePlayer(playerIndex);
                var slot = _lobby.GetSlot(playerIndex);
                var pi = instance.GetComponent<PlayerInput>();
                if (pi != null && slot.PrimaryDevice != null)
                {
                    try
                    {
                        if (slot.SecondaryDevice != null)
                            pi.SwitchCurrentControlScheme(slot.SchemeName, slot.PrimaryDevice, slot.SecondaryDevice);
                        else
                            pi.SwitchCurrentControlScheme(slot.SchemeName, slot.PrimaryDevice);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[HubLobby] Control scheme assign failed: {ex.Message}");
                    }
                }

                var health = instance.GetComponent<Health>();
                if (health != null)
                    _healthBars[playerIndex] = PlayerOverheadHealthBar.Attach(instance.transform, health);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                instance.transform.SetParent(_arena.Root, false);
                instance.transform.position = pos;
                Destroy(instance.GetComponent<Collider>());
            }

            _avatars[playerIndex] = instance;
        }

        private void RefreshJoinHud()
        {
            if (_joinHud == null) return;
            for (int i = 0; i < LocalCoopLobby.MaxPlayers; i++)
                _joinHud.SetSlotVisible(i, !_lobby.GetSlot(i).Joined);
        }

        private void PollTomeInteract()
        {
            if (_tome == null || _spellUi == null) return;

            int player = -1;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var world = Camera.main != null
                    ? Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue())
                    : Vector3.zero;
                if (_tome.ContainsPoint(world))
                {
                    if (_lobby.Claims.TryGetPlayerForDevice(Keyboard.current, out int kbPlayer))
                        player = kbPlayer;
                    else
                        player = FirstJoinedPlayer();
                }
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad == null || !pad.buttonSouth.wasPressedThisFrame) continue;
                if (!_lobby.Claims.TryGetPlayerForDevice(pad, out int idx)) continue;
                if (_avatars[idx] == null) continue;
                float dist = Vector2.Distance(_avatars[idx].transform.position, _tome.transform.position);
                if (dist <= 2.2f)
                {
                    player = idx;
                    break;
                }
            }

            if (player < 0) return;
            _tome.PlayOpen(() => _spellUi.ShowForPlayer(player));
        }

        private int FirstJoinedPlayer()
        {
            for (int i = 0; i < LocalCoopLobby.MaxPlayers; i++)
                if (_lobby.GetSlot(i).Joined)
                    return i;
            return 0;
        }

        private void PollExit()
        {
            if (_startingRun || _exitCollider == null || _lobby.JoinedCount == 0)
                return;

            var waiting = new List<int>();
            for (int i = 0; i < LocalCoopLobby.MaxPlayers; i++)
            {
                if (!_lobby.GetSlot(i).Joined) continue;
                var avatar = _avatars[i];
                if (avatar == null)
                {
                    waiting.Add(i);
                    continue;
                }

                if (!_exitCollider.OverlapPoint(avatar.transform.position))
                    waiting.Add(i);
            }

            if (waiting.Count == 0)
                StartRun();
        }

        private void StartRun()
        {
            if (_startingRun) return;
            _startingRun = true;
            var ctx = GameContext.Instance;
            if (ctx == null) return;

            int count = Mathf.Max(1, _lobby.JoinedCount);
            ctx.Save.Current.settings.localPlayerCount = count;
            ctx.Save.Current.meta.preferredPreCapitalNodes = _preCapital;
            ctx.Runs.BeginWorldRun(_preCapital, count);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                if (_lobby.Claims.TryGetPlayerForDevice(device, out int idx))
                    LeavePlayer(idx);
            }
        }
    }
}
