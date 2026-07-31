using RealmShards.Core;
using RealmShards.Magic;
using RealmShards.Save;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Spend Arcane Vestiges to unlock city/school spells after champion defeat.
    /// </summary>
    public sealed class ArcaneCoreUnlockScreen : MonoBehaviour
    {
        private bool _open;
        private System.Action _onClosed;

        public static void Show(string[] abilityIds, int[] costs, System.Action onClosed)
        {
            var existing = FindFirstObjectByType<ArcaneCoreUnlockScreen>();
            if (existing != null)
                Destroy(existing.gameObject);

            var canvas = UiFactory.CreateScreenCanvas("ArcaneCoreUnlockUI", 300);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            var screen = canvas.gameObject.AddComponent<ArcaneCoreUnlockScreen>();
            screen._onClosed = onClosed;
            screen.Build(abilityIds, costs);
        }

        private void Build(string[] abilityIds, int[] costs)
        {
            _open = true;
            Combat.HitStop.SetMenuPaused(true);

            UiFactory.AddPanel(transform, "Bg", new Color(0.05f, 0.06f, 0.1f, 0.92f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.AddText(transform, "Title", "Arcane Core", 44, TextAnchor.MiddleCenter,
                new Color(0.55f, 0.85f, 1f),
                new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f), Vector2.zero, Vector2.zero);

            var ctx = GameContext.Instance;
            int vestiges = ctx != null ? ctx.Progression.ArcaneVestiges : 0;
            var vestigeLabel = UiFactory.AddText(transform, "Vestiges", $"Arcane Vestiges: {vestiges}", 22,
                TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.2f, 0.78f), new Vector2(0.8f, 0.86f), Vector2.zero, Vector2.zero);

            if (abilityIds == null || abilityIds.Length == 0)
            {
                abilityIds = new[]
                {
                    ContentIdDefaults.AbilityArcanePulse,
                    ContentIdDefaults.AbilityBlinkStep,
                    ContentIdDefaults.AbilityGildedFlare,
                    ContentIdDefaults.AbilityAshenCinder
                };
                costs = new[] { 15, 20, 18, 18 };
            }

            float y = 0.72f;
            var navEntries = new List<MenuNavigator.Entry>();
            for (int i = 0; i < abilityIds.Length; i++)
            {
                string id = abilityIds[i];
                int cost = costs != null && i < costs.Length ? costs[i] : 15;
                bool owned = ctx != null && ctx.Progression.IsAbilityUnlocked(id);
                string name = ctx != null ? ctx.Content.GetDisplayName(id, id) : id;
                string label = owned ? $"{name} — OWNED" : $"{name} — {cost} Vestiges";
                Color col = owned
                    ? new Color(0.25f, 0.35f, 0.3f, 1f)
                    : (vestiges >= cost ? new Color(0.15f, 0.4f, 0.45f, 1f) : new Color(0.3f, 0.2f, 0.2f, 1f));

                float yMin = y - 0.08f;
                var btn = UiFactory.AddButton(transform, $"Unlock_{i}", label,
                    new Vector2(0.18f, yMin), new Vector2(0.82f, y), Vector2.zero, Vector2.zero, col);
                btn.GetComponentInChildren<Text>().fontSize = 20;
                int capturedCost = cost;
                string capturedId = id;
                bool capturedOwned = owned;
                btn.onClick.AddListener(() =>
                {
                    if (capturedOwned || ctx == null) return;
                    if (ctx.Progression.TryPurchaseAbilityUnlock(capturedId, capturedCost, out var fail))
                    {
                        vestigeLabel.text = $"Arcane Vestiges: {ctx.Progression.ArcaneVestiges}";
                        btn.GetComponentInChildren<Text>().text = $"{ctx.Content.GetDisplayName(capturedId, capturedId)} — OWNED";
                        btn.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.3f, 1f);
                    }
                    else
                    {
                        vestigeLabel.text = fail ?? "Cannot unlock.";
                    }
                });
                navEntries.Add(new MenuNavigator.Entry
                {
                    Visual = btn.GetComponent<RectTransform>(),
                    Selectable = btn,
                    OnConfirm = () => btn.onClick.Invoke()
                });
                y -= 0.09f;
            }

            var cont = UiFactory.AddButton(transform, "Continue", "Continue",
                new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.14f), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.45f, 0.3f, 1f));
            cont.onClick.AddListener(Close);
            navEntries.Add(new MenuNavigator.Entry
            {
                Visual = cont.GetComponent<RectTransform>(),
                Selectable = cont,
                OnConfirm = Close
            });

            var nav = gameObject.AddComponent<MenuNavigator>();
            nav.Configure(navEntries, onCancel: Close, startIndex: navEntries.Count - 1);
            nav.Activate(navEntries.Count - 1);
        }

        private void Close()
        {
            if (!_open) return;
            _open = false;
            Combat.HitStop.EnsureRunningTimeScale();
            var cb = _onClosed;
            Destroy(gameObject);
            cb?.Invoke();
        }
    }
}
