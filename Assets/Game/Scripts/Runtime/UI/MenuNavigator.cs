using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Console-style menu focus: up/down (or W/S), left/right to adjust,
    /// confirm (A / Enter / Space / E), cancel (B / Esc).
    /// </summary>
    public sealed class MenuNavigator : MonoBehaviour
    {
        public sealed class Entry
        {
            public RectTransform Visual;
            public Selectable Selectable;
            public Action OnConfirm;
            public Action OnLeft;
            public Action OnRight;
            public Action OnFocused;
            public bool Enabled = true;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<Vector3> _baseScales = new List<Vector3>();
        private readonly List<Color?> _baseImageColors = new List<Color?>();
        private int _index;
        private bool _active;
        private float _moveCooldown;
        private float _adjustCooldown;
        private Action _onCancel;
        private const float SelectedScale = 1.08f;
        private static readonly Color SelectedTint = new Color(1.18f, 1.12f, 1.35f, 1f);
        private const float MoveRepeat = 0.18f;
        private const float AdjustRepeat = 0.12f;

        public bool IsActive => _active;
        public int Index => _index;
        public int Count => _entries.Count;

        public void Configure(IList<Entry> entries, Action onCancel = null, int startIndex = 0)
        {
            ClearVisuals();
            _entries.Clear();
            _baseScales.Clear();
            _baseImageColors.Clear();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i] == null) continue;
                    _entries.Add(entries[i]);
                    var visual = entries[i].Visual;
                    _baseScales.Add(visual != null ? visual.localScale : Vector3.one);
                    Image img = null;
                    if (visual != null)
                        img = visual.GetComponent<Image>();
                    _baseImageColors.Add(img != null ? img.color : (Color?)null);
                }
            }

            _onCancel = onCancel;
            _index = Mathf.Clamp(startIndex, 0, Mathf.Max(0, _entries.Count - 1));
        }

        public void Activate(int? startIndex = null)
        {
            _active = true;
            _moveCooldown = 0.08f;
            _adjustCooldown = 0.08f;
            if (startIndex.HasValue && _entries.Count > 0)
                _index = Mathf.Clamp(startIndex.Value, 0, _entries.Count - 1);
            SuppressEventSystemNavigation(true);
            ApplyFocus(true);
        }

        public void Deactivate()
        {
            if (!_active && _entries.Count == 0)
                return;
            _active = false;
            ClearVisuals();
            SuppressEventSystemNavigation(false);
        }

        private void Update()
        {
            if (!_active || _entries.Count == 0)
                return;

            _moveCooldown = Mathf.Max(0f, _moveCooldown - Time.unscaledDeltaTime);
            _adjustCooldown = Mathf.Max(0f, _adjustCooldown - Time.unscaledDeltaTime);

            if (MenuInput.CancelPressed())
            {
                _onCancel?.Invoke();
                return;
            }

            int move = MenuInput.VerticalPressed();
            if (move != 0 && _moveCooldown <= 0f)
            {
                MoveSelection(move > 0 ? -1 : 1);
                _moveCooldown = MoveRepeat;
            }

            int horiz = MenuInput.HorizontalPressed();
            if (horiz != 0 && _adjustCooldown <= 0f)
            {
                var entry = Current;
                if (entry != null && (entry.OnLeft != null || entry.OnRight != null))
                {
                    if (horiz < 0) entry.OnLeft?.Invoke();
                    else entry.OnRight?.Invoke();
                }
                else
                {
                    MoveSelection(horiz > 0 ? 1 : -1);
                }

                _adjustCooldown = AdjustRepeat;
            }

            if (MenuInput.ConfirmPressed())
                Current?.OnConfirm?.Invoke();
        }

        private Entry Current =>
            _index >= 0 && _index < _entries.Count ? _entries[_index] : null;

        private void MoveSelection(int delta)
        {
            if (_entries.Count == 0) return;
            int safety = _entries.Count;
            int next = _index;
            do
            {
                next = (next + delta + _entries.Count) % _entries.Count;
                safety--;
            } while (!_entries[next].Enabled && safety > 0);

            if (!_entries[next].Enabled)
                return;

            _index = next;
            ApplyFocus(true);
        }

        private void ApplyFocus(bool announce)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var visual = _entries[i].Visual;
                if (visual == null) continue;
                var baseScale = i < _baseScales.Count ? _baseScales[i] : Vector3.one;
                visual.localScale = i == _index ? baseScale * SelectedScale : baseScale;

                var img = visual.GetComponent<Image>();
                if (img != null && i < _baseImageColors.Count && _baseImageColors[i].HasValue)
                {
                    var baseColor = _baseImageColors[i].Value;
                    img.color = i == _index
                        ? new Color(
                            Mathf.Min(1f, baseColor.r * SelectedTint.r),
                            Mathf.Min(1f, baseColor.g * SelectedTint.g),
                            Mathf.Min(1f, baseColor.b * SelectedTint.b),
                            baseColor.a)
                        : baseColor;
                }
            }

            var entry = Current;
            if (entry == null) return;

            if (entry.Selectable != null)
            {
                Core.GameContext.EnsureEventSystem();
                entry.Selectable.Select();
            }

            if (announce)
                entry.OnFocused?.Invoke();
        }

        private void ClearVisuals()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var visual = _entries[i].Visual;
                if (visual == null) continue;
                var baseScale = i < _baseScales.Count ? _baseScales[i] : Vector3.one;
                visual.localScale = baseScale;

                var img = visual.GetComponent<Image>();
                if (img != null && i < _baseImageColors.Count && _baseImageColors[i].HasValue)
                    img.color = _baseImageColors[i].Value;
            }
        }

        private static void SuppressEventSystemNavigation(bool suppress)
        {
            var es = EventSystem.current;
            if (es != null)
                es.sendNavigationEvents = !suppress;
        }

        private void OnDisable()
        {
            if (_active)
                Deactivate();
        }
    }

    /// <summary>Shared console-style menu input polling (keyboard + gamepad).</summary>
    public static class MenuInput
    {
        private const float StickThreshold = 0.55f;
        private static float _prevStickY;
        private static float _prevStickX;

        public static int VerticalPressed()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) return 1;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) return -1;
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad == null) continue;
                if (pad.dpad.up.wasPressedThisFrame) return 1;
                if (pad.dpad.down.wasPressedThisFrame) return -1;
            }

            return StickVerticalEdge();
        }

        public static int HorizontalPressed()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) return -1;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) return 1;
            }

            foreach (var pad in Gamepad.all)
            {
                if (pad == null) continue;
                if (pad.dpad.left.wasPressedThisFrame) return -1;
                if (pad.dpad.right.wasPressedThisFrame) return 1;
            }

            return StickHorizontalEdge();
        }

        public static bool ConfirmPressed()
        {
            var kb = Keyboard.current;
            if (kb != null &&
                (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                 kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame))
                return true;

            foreach (var pad in Gamepad.all)
            {
                if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame))
                    return true;
            }

            return false;
        }

        public static bool CancelPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame))
                return true;

            foreach (var pad in Gamepad.all)
            {
                if (pad != null && pad.buttonEast.wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        private static int StickVerticalEdge()
        {
            float y = 0f;
            foreach (var pad in Gamepad.all)
            {
                if (pad == null) continue;
                float v = pad.leftStick.ReadValue().y;
                if (Mathf.Abs(v) > Mathf.Abs(y)) y = v;
            }

            int result = 0;
            if (_prevStickY <= StickThreshold && y > StickThreshold) result = 1;
            else if (_prevStickY >= -StickThreshold && y < -StickThreshold) result = -1;
            _prevStickY = y;
            return result;
        }

        private static int StickHorizontalEdge()
        {
            float x = 0f;
            foreach (var pad in Gamepad.all)
            {
                if (pad == null) continue;
                float v = pad.leftStick.ReadValue().x;
                if (Mathf.Abs(v) > Mathf.Abs(x)) x = v;
            }

            int result = 0;
            if (_prevStickX <= StickThreshold && x > StickThreshold) result = 1;
            else if (_prevStickX >= -StickThreshold && x < -StickThreshold) result = -1;
            _prevStickX = x;
            return result;
        }
    }
}
