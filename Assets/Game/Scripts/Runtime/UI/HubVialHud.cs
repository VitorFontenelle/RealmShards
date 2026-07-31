using RealmShards.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Persistent bottom bar showing the player's vial balance in the hub lobby.
    /// </summary>
    public sealed class HubVialHud : MonoBehaviour
    {
        private GameObject _root;
        private Text _countLabel;
        private Image _icon;

        public static HubVialHud EnsurePresent()
        {
            var existing = FindFirstObjectByType<HubVialHud>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(HubVialHud));
            return go.AddComponent<HubVialHud>();
        }

        private void Awake() => Build();

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("HubVialHud", 125);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            _root = canvas.gameObject;

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvas.transform, false);
            var safeRt = safe.GetComponent<RectTransform>();
            UiScaleConfig.ApplySafeArea(safeRt);

            var bar = new GameObject("VialBar", typeof(RectTransform));
            bar.transform.SetParent(safe.transform, false);
            var barRt = bar.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.38f, 0.02f);
            barRt.anchorMax = new Vector2(0.62f, 0.1f);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            _icon = UiFactory.AddSprite(bar.transform, "VialIcon", LoadVialSprite(),
                new Vector2(0f, 0.1f), new Vector2(0.22f, 0.9f),
                Vector2.zero, Vector2.zero, preserveAspect: true);
            _icon.color = Color.white;

            _countLabel = UiFactory.AddText(bar.transform, "VialCount", "0", 26, TextAnchor.MiddleLeft,
                new Color(0.95f, 0.92f, 0.78f, 1f),
                new Vector2(0.24f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, UiFonts.MenuBold);
            var outline = _countLabel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.15f, 0.55f, 0.7f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);

            Refresh();
        }

        public void Refresh()
        {
            if (_countLabel == null)
                return;
            int vials = GameContext.Instance?.Progression?.Vials ?? 0;
            _countLabel.text = vials.ToString();
        }

        public void Show()
        {
            if (_root != null)
                _root.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private static Sprite LoadVialSprite()
        {
            var tex = Resources.Load<Texture2D>("UI/currency_vial");
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
