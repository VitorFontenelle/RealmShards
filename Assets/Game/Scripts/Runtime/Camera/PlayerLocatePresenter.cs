using RealmShards.Input;
using RealmShards.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.CameraSystem
{
    /// <summary>
    /// Off-screen edge arrows + camera pulse when <see cref="PlayerLocateSignal"/> fires.
    /// </summary>
    public sealed class PlayerLocatePresenter : MonoBehaviour
    {
        [SerializeField] private float pulseDuration = 0.55f;
        [SerializeField] private float pulseOrthoBoost = 1.35f;
        [SerializeField] private float edgePadding = 40f;

        private Canvas _canvas;
        private readonly System.Collections.Generic.Dictionary<int, RectTransform> _arrows =
            new System.Collections.Generic.Dictionary<int, RectTransform>();
        private float _pulseUntil;
        private Camera _cam;
        private SharedOrthoCamera _shared;

        public static void EnsurePresent()
        {
            if (Object.FindFirstObjectByType<PlayerLocatePresenter>() != null)
                return;
            var go = new GameObject(nameof(PlayerLocatePresenter));
            go.AddComponent<PlayerLocatePresenter>();
        }

        private void OnEnable() => PlayerLocateSignal.Located += OnLocated;
        private void OnDisable() => PlayerLocateSignal.Located -= OnLocated;

        private void Start()
        {
            _cam = Camera.main;
            if (_cam != null)
                _shared = _cam.GetComponent<SharedOrthoCamera>();

            var ui = UiFactory.CreateScreenCanvas("LocateHUD", 180);
            UiScaleConfig.Apply(ui.GetComponent<CanvasScaler>());
            ui.transform.SetParent(transform, false);
            _canvas = ui;
        }

        private void OnLocated(PlayerInputBridge bridge)
        {
            if (bridge == null) return;
            _pulseUntil = Time.unscaledTime + pulseDuration;
            _shared?.PulseLocate(bridge.transform, pulseDuration, pulseOrthoBoost);
            EnsureArrow(bridge);
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || _canvas == null) return;

            var players = Enemies.PlayerTargetRegistry.Collect();
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p?.Transform == null || !p.IsAlive) continue;
                UpdateOffscreenArrow(p.Transform.GetEntityId().GetHashCode(), p.Transform.position, i);
            }
        }

        private void EnsureArrow(PlayerInputBridge bridge)
        {
            int id = bridge.transform.GetEntityId().GetHashCode();
            if (_arrows.ContainsKey(id)) return;
            var img = UiFactory.AddPanel(_canvas.transform, $"Arrow_{id}",
                new Color(1f, 0.9f, 0.4f, 0.9f),
                new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f),
                Vector2.zero, Vector2.zero);
            img.rectTransform.sizeDelta = new Vector2(28f, 18f);
            _arrows[id] = img.rectTransform;
        }

        private void UpdateOffscreenArrow(int id, Vector3 worldPos, int colorIndex)
        {
            if (!_arrows.TryGetValue(id, out var arrow))
            {
                var img = UiFactory.AddPanel(_canvas.transform, $"Arrow_{id}",
                    PlayerTint(colorIndex),
                    new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f),
                    Vector2.zero, Vector2.zero);
                img.rectTransform.sizeDelta = new Vector2(22f, 14f);
                arrow = img.rectTransform;
                _arrows[id] = arrow;
            }

            Vector3 vp = _cam.WorldToViewportPoint(worldPos);
            bool off = vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f;
            bool pulsing = Time.unscaledTime <= _pulseUntil;
            arrow.gameObject.SetActive(off || pulsing);

            if (!arrow.gameObject.activeSelf) return;

            float x = Mathf.Clamp(vp.x, 0.05f, 0.95f);
            float y = Mathf.Clamp(vp.y, 0.05f, 0.95f);
            if (vp.z < 0f)
            {
                x = 1f - x;
                y = 1f - y;
            }

            arrow.anchorMin = arrow.anchorMax = new Vector2(x, y);
            arrow.anchoredPosition = Vector2.zero;

            Vector2 dir = new Vector2(x - 0.5f, y - 0.5f);
            if (dir.sqrMagnitude > 0.001f)
            {
                float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                arrow.localEulerAngles = new Vector3(0f, 0f, ang);
            }

            _ = edgePadding;
        }

        private static Color PlayerTint(int i) => i switch
        {
            0 => new Color(0.8f, 0.5f, 1f, 0.85f),
            1 => new Color(0.4f, 0.9f, 0.5f, 0.85f),
            2 => new Color(0.95f, 0.85f, 0.3f, 0.85f),
            _ => new Color(0.95f, 0.4f, 0.4f, 0.85f)
        };
    }
}
