using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Briefly flashes current vial and coin totals after defeating opponents in a run.
    /// </summary>
    public sealed class RunCurrencyFlashHud : MonoBehaviour
    {
        private const float FadeDuration = 2.4f;

        private static RunCurrencyFlashHud _instance;

        private GameObject _root;
        private CanvasGroup _group;
        private Text _vialLabel;
        private Text _coinLabel;
        private Image _vialIcon;
        private Image _coinIcon;
        private float _fadeTimer;

        public static void Notify(int vials, int coins)
        {
            var hud = EnsurePresent();
            hud.ShowValues(vials, coins);
        }

        public static RunCurrencyFlashHud EnsurePresent()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject(nameof(RunCurrencyFlashHud));
            _instance = go.AddComponent<RunCurrencyFlashHud>();
            return _instance;
        }

        private void Awake() => Build();

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("RunCurrencyFlashHud", 160);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            _root = canvas.gameObject;
            _group = UiFactory.AddCanvasGroup(_root, 0f, interactable: false, blocksRaycasts: false);

            var safe = new GameObject("SafeArea", typeof(RectTransform));
            safe.transform.SetParent(canvas.transform, false);
            var safeRt = safe.GetComponent<RectTransform>();
            UiScaleConfig.ApplySafeArea(safeRt);

            var panel = new GameObject("CurrencyPanel", typeof(RectTransform));
            panel.transform.SetParent(safe.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.32f, 0.04f);
            panelRt.anchorMax = new Vector2(0.68f, 0.14f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            _vialIcon = UiFactory.AddSprite(panel.transform, "VialIcon", LoadSprite("UI/currency_vial"),
                new Vector2(0.02f, 0.12f), new Vector2(0.14f, 0.88f),
                Vector2.zero, Vector2.zero, preserveAspect: true);

            _vialLabel = UiFactory.AddText(panel.transform, "VialCount", "0", 24, TextAnchor.MiddleLeft,
                new Color(0.92f, 0.88f, 1f, 1f),
                new Vector2(0.15f, 0f), new Vector2(0.48f, 1f),
                Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            _coinIcon = UiFactory.AddSprite(panel.transform, "CoinIcon", LoadSprite("UI/currency_coin"),
                new Vector2(0.5f, 0.12f), new Vector2(0.62f, 0.88f),
                Vector2.zero, Vector2.zero, preserveAspect: true);

            _coinLabel = UiFactory.AddText(panel.transform, "CoinCount", "0", 24, TextAnchor.MiddleLeft,
                new Color(1f, 0.9f, 0.55f, 1f),
                new Vector2(0.63f, 0f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            _root.SetActive(false);
        }

        private void Update()
        {
            if (_fadeTimer <= 0f || _group == null)
                return;

            _fadeTimer -= Time.unscaledDeltaTime;
            _group.alpha = Mathf.Clamp01(_fadeTimer / FadeDuration);
            if (_fadeTimer <= 0f)
                _root.SetActive(false);
        }

        private void ShowValues(int vials, int coins)
        {
            if (_vialLabel != null)
                _vialLabel.text = vials.ToString();
            if (_coinLabel != null)
                _coinLabel.text = coins.ToString();

            _fadeTimer = FadeDuration;
            _group.alpha = 1f;
            _root.SetActive(true);
        }

        private static Sprite LoadSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
