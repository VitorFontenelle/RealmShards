using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using RealmShards.Input;

namespace RealmShards.UI
{
    /// <summary>
    /// Settings → Controls rebind UI. Supports Keyboard&Mouse and Gamepad schemes.
    /// Move stick axes are listed as informational (not rebound as a whole); WASD composite parts can be rebound.
    /// </summary>
    public sealed class ControlsRebindScreen : MonoBehaviour
    {
        private static readonly string[] RebindActions =
        {
            "BasicAbility", "Ability1", "Ability2", "Ability3",
            "Dash", "Interact", "DropItem", "Pause", "Join", "Confirm", "Cancel", "LocatePlayer"
        };

        private InputActionAsset _actions;
        private BindingOverridesService _bindings;
        private GameObject _panel;
        private Text _status;
        private Text _list;
        private Coroutine _rebindRoutine;
        private InputActionRebindingExtensions.RebindingOperation _operation;
        private MenuNavigator _nav;
        private readonly List<MenuNavigator.Entry> _navEntries = new List<MenuNavigator.Entry>();
        private System.Action _onClosed;

        public static ControlsRebindScreen EnsurePresent(Transform parent, InputActionAsset actions, BindingOverridesService bindings)
        {
            var existing = Object.FindFirstObjectByType<ControlsRebindScreen>();
            if (existing != null)
            {
                existing._actions = actions;
                existing._bindings = bindings;
                return existing;
            }

            var go = new GameObject("ControlsRebindScreen");
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<ControlsRebindScreen>();
            screen._actions = actions;
            screen._bindings = bindings;
            screen.Build();
            screen.Hide();
            return screen;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("ControlsUI", 250);
            canvas.transform.SetParent(transform, false);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());

            _panel = new GameObject("Panel", typeof(RectTransform));
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.GetComponent<RectTransform>();
            UiScaleConfig.ApplySafeArea(rt);
            UiFactory.AddPanel(_panel.transform, "Bg", new Color(0.06f, 0.07f, 0.1f, 0.96f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.AddText(_panel.transform, "Title", "Controls", 42, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.1f, 0.88f), new Vector2(0.9f, 0.98f), Vector2.zero, Vector2.zero);

            _status = UiFactory.AddText(_panel.transform, "Status",
                "Click / Submit a row, then press a new button. Stick Move/Aim axes stay on defaults.",
                18, TextAnchor.UpperCenter, new Color(0.8f, 0.85f, 0.9f),
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);

            _list = UiFactory.AddText(_panel.transform, "List", string.Empty, 20, TextAnchor.UpperLeft,
                new Color(0.9f, 0.92f, 0.95f),
                new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.76f), Vector2.zero, Vector2.zero);

            float y = 0.18f;
            foreach (var actionName in RebindActions)
            {
                string captured = actionName;
                float yMax = y;
                float yMin = y - 0.045f;
                var btn = UiFactory.AddButton(_panel.transform, $"Rebind_{actionName}", actionName,
                    new Vector2(0.55f, yMin), new Vector2(0.92f, yMax), Vector2.zero, Vector2.zero,
                    new Color(0.16f, 0.22f, 0.3f, 1f));
                btn.GetComponentInChildren<Text>().fontSize = 18;
                btn.onClick.AddListener(() => StartRebind(captured));
                _navEntries.Add(new MenuNavigator.Entry
                {
                    Visual = btn.GetComponent<RectTransform>(),
                    Selectable = btn,
                    OnConfirm = () => StartRebind(captured)
                });
                y -= 0.048f;
            }

            var reset = UiFactory.AddButton(_panel.transform, "Reset", "Reset Defaults",
                new Vector2(0.08f, 0.04f), new Vector2(0.32f, 0.12f), Vector2.zero, Vector2.zero,
                new Color(0.4f, 0.25f, 0.2f, 1f));
            reset.onClick.AddListener(OnReset);
            _navEntries.Add(new MenuNavigator.Entry
            {
                Visual = reset.GetComponent<RectTransform>(),
                Selectable = reset,
                OnConfirm = OnReset
            });

            var close = UiFactory.AddButton(_panel.transform, "Close", "Back",
                new Vector2(0.68f, 0.04f), new Vector2(0.92f, 0.12f), Vector2.zero, Vector2.zero);
            close.onClick.AddListener(Hide);
            _navEntries.Add(new MenuNavigator.Entry
            {
                Visual = close.GetComponent<RectTransform>(),
                Selectable = close,
                OnConfirm = Hide
            });

            _nav = gameObject.AddComponent<MenuNavigator>();
            RefreshList();
        }

        public void Show(System.Action onClosed = null)
        {
            _onClosed = onClosed;
            if (_panel != null) _panel.SetActive(true);
            gameObject.SetActive(true);
            RefreshList();
            _nav.Configure(_navEntries, onCancel: Hide, startIndex: 0);
            _nav.Activate(0);
        }

        public void Hide()
        {
            CancelRebind();
            _nav?.Deactivate();
            if (_panel != null) _panel.SetActive(false);
            var closed = _onClosed;
            _onClosed = null;
            closed?.Invoke();
        }

        private void OnReset()
        {
            CancelRebind();
            _bindings?.ResetToDefaults();
            _status.text = "Bindings reset to defaults.";
            RefreshList();
        }

        private void StartRebind(string actionName)
        {
            if (_actions == null) return;
            CancelRebind();
            var action = _actions.FindAction($"Player/{actionName}", false);
            if (action == null)
            {
                _status.text = $"Missing action {actionName}";
                return;
            }

            // Prefer gamepad binding index if a pad is present, else keyboard.
            int bindingIndex = FindPreferredBindingIndex(action);
            if (bindingIndex < 0)
            {
                _status.text = "No rebindable binding (Move composites: rebind not offered for whole stick).";
                return;
            }

            _status.text = $"Rebinding {actionName}… press a button (Esc / B cancel)";
            _nav?.Deactivate();
            action.Disable();
            _operation = action.PerformInteractiveRebinding(bindingIndex)
                .WithControlsExcluding("<Mouse>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    string replaced = null;
                    var newBinding = action.bindings[bindingIndex];
                    _bindings?.ApplyBindingWithConflictReplace(action, bindingIndex, newBinding, out replaced);
                    action.Enable();
                    _status.text = replaced != null
                        ? $"Bound {actionName}. Replaced conflict on {replaced}."
                        : $"Bound {actionName}.";
                    _operation?.Dispose();
                    _operation = null;
                    RefreshList();
                    RestoreNav();
                })
                .OnCancel(op =>
                {
                    action.Enable();
                    _status.text = "Rebind cancelled.";
                    _operation?.Dispose();
                    _operation = null;
                    RestoreNav();
                })
                .Start();
        }

        private void RestoreNav()
        {
            if (_panel == null || !_panel.activeSelf) return;
            _nav.Configure(_navEntries, onCancel: Hide, startIndex: 0);
            _nav.Activate(0);
        }

        private static int FindPreferredBindingIndex(InputAction action)
        {
            bool preferPad = Gamepad.current != null;
            int fallback = -1;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var b = action.bindings[i];
                if (b.isComposite || b.isPartOfComposite)
                    continue;
                string groups = b.groups ?? string.Empty;
                if (preferPad && groups.Contains("Gamepad"))
                    return i;
                if (!preferPad && groups.Contains("Keyboard"))
                    return i;
                if (fallback < 0)
                    fallback = i;
            }

            return fallback;
        }

        private void CancelRebind()
        {
            if (_operation != null)
            {
                _operation.Cancel();
                _operation.Dispose();
                _operation = null;
            }
        }

        private void RefreshList()
        {
            if (_list == null || _actions == null) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Move: Left Stick / WASD (stick not rebound as a block)");
            sb.AppendLine("Aim: Right Stick / Mouse");
            sb.AppendLine();
            foreach (var name in RebindActions)
            {
                var action = _actions.FindAction($"Player/{name}", false);
                if (action == null) continue;
                sb.Append(name).Append(": ");
                bool first = true;
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var b = action.bindings[i];
                    if (b.isComposite || b.isPartOfComposite) continue;
                    if (!first) sb.Append(" | ");
                    sb.Append(action.GetBindingDisplayString(i));
                    first = false;
                }

                sb.AppendLine();
            }

            _list.text = sb.ToString();
        }

        private void OnDestroy()
        {
            CancelRebind();
        }
    }
}
