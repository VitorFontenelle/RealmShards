using RealmShards.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Corner join prompts for the visual hub lobby.
    /// </summary>
    public sealed class HubLobbyJoinHud : MonoBehaviour
    {
        private readonly Text[] _cornerLabels = new Text[4];
        private GameObject _root;

        public static HubLobbyJoinHud EnsurePresent()
        {
            var existing = FindFirstObjectByType<HubLobbyJoinHud>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(HubLobbyJoinHud));
            return go.AddComponent<HubLobbyJoinHud>();
        }

        private void Awake() => Build();

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("HubJoinHud", 120);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            _root = canvas.gameObject;

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvas.transform, false);
            var safeRt = safe.GetComponent<RectTransform>();
            UiScaleConfig.ApplySafeArea(safeRt);

            Vector2[] mins =
            {
                new Vector2(0.02f, 0.02f),
                new Vector2(0.62f, 0.02f),
                new Vector2(0.02f, 0.82f),
                new Vector2(0.62f, 0.82f)
            };
            Vector2[] maxs =
            {
                new Vector2(0.36f, 0.16f),
                new Vector2(0.98f, 0.16f),
                new Vector2(0.36f, 0.96f),
                new Vector2(0.98f, 0.96f)
            };

            for (int i = 0; i < 4; i++)
            {
                _cornerLabels[i] = UiFactory.AddText(safe.transform, $"JoinP{i + 1}",
                    $"P{i + 1}\nPRESS TO JOIN", 18, TextAnchor.MiddleCenter,
                    new Color(0.95f, 0.92f, 0.75f, 1f),
                    mins[i], maxs[i], Vector2.zero, Vector2.zero, UiFonts.MenuBold);
                var outline = _cornerLabels[i].gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0.45f, 0.2f, 0.75f, 0.65f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);
            }
        }

        public void SetSlotVisible(int playerIndex, bool visible)
        {
            if (playerIndex < 0 || playerIndex >= _cornerLabels.Length || _cornerLabels[playerIndex] == null)
                return;
            _cornerLabels[playerIndex].gameObject.SetActive(visible);
        }

        public void Show() { if (_root != null) _root.SetActive(true); }
        public void Hide() { if (_root != null) _root.SetActive(false); }
    }
}
