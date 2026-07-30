using System.Collections.Generic;
using RealmShards.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Per-player HP, ability cooldown pips, short inventory strip. Tuned for 1280×800.
    /// </summary>
    public sealed class CombatHud : MonoBehaviour
    {
        private sealed class PlayerRow
        {
            public Health Health;
            public AbilityCaster Caster;
            public PlayerInventory Inventory;
            public Image HpFill;
            public Text HpLabel;
            public readonly Image[] CdFills = new Image[AbilityCaster.SlotCount];
            public readonly Text[] InvSlots = new Text[4];
        }

        private readonly List<PlayerRow> _rows = new List<PlayerRow>(4);
        private Text _roomLabel;
        private float _refreshTimer;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<CombatHud>() != null)
                return;
            var go = new GameObject(nameof(CombatHud));
            go.AddComponent<CombatHud>();
        }

        private void Start()
        {
            Build();
            Invoke(nameof(RebuildPlayers), 0.35f);
        }

        private void Update()
        {
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 0.5f;
                if (_rows.Count == 0)
                    RebuildPlayers();
            }

            for (int i = 0; i < _rows.Count; i++)
                TickRow(_rows[i]);

            var director = FindFirstObjectByType<Rooms.CityRunDirector>();
            if (_roomLabel != null && director != null)
                _roomLabel.text = $"Room {director.RoomIndex + 1}/{director.TotalRooms}" +
                                  (director.IsChampionRoom ? " · Champion" : string.Empty);
        }

        private void RebuildPlayers()
        {
            _rows.Clear();
            var players = PlayerTargetRegistry.Collect();
            for (int i = 0; i < players.Count && i < 4; i++)
            {
                var t = players[i]?.Transform;
                if (t == null) continue;
                var health = t.GetComponent<Health>();
                if (health == null) continue;
                var row = new PlayerRow
                {
                    Health = health,
                    Caster = t.GetComponent<AbilityCaster>(),
                    Inventory = t.GetComponent<PlayerInventory>()
                };
                BuildPlayerStrip(i, row);
                _rows.Add(row);
            }
        }

        private Transform _canvasRoot;

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("CombatHUD", 150);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            canvas.transform.SetParent(transform, false);
            _canvasRoot = canvas.transform;

            _roomLabel = UiFactory.AddText(_canvasRoot, "Room", "Room —", 14,
                TextAnchor.UpperRight, new Color(0.9f, 0.9f, 1f, 0.85f),
                new Vector2(0.72f, 0.94f), new Vector2(0.98f, 0.99f), Vector2.zero, Vector2.zero);
        }

        private void BuildPlayerStrip(int index, PlayerRow row)
        {
            float yMax = 0.18f - index * 0.09f;
            float yMin = yMax - 0.075f;
            if (yMin < 0.02f) return;

            UiFactory.AddPanel(_canvasRoot, $"P{index + 1}Bg",
                new Color(0.05f, 0.06f, 0.09f, 0.55f),
                new Vector2(0.02f, yMin), new Vector2(0.42f, yMax),
                Vector2.zero, Vector2.zero);

            UiFactory.AddText(_canvasRoot, $"P{index + 1}Tag", $"P{index + 1}", 13,
                TextAnchor.MiddleLeft, PlayerColor(index),
                new Vector2(0.025f, yMin), new Vector2(0.06f, yMax), Vector2.zero, Vector2.zero);

            var hpBg = UiFactory.AddPanel(_canvasRoot, $"P{index + 1}HpBg",
                new Color(0.15f, 0.12f, 0.12f, 0.9f),
                new Vector2(0.06f, yMin + 0.035f), new Vector2(0.28f, yMax - 0.01f),
                Vector2.zero, Vector2.zero);

            row.HpFill = UiFactory.AddPanel(hpBg.transform, "Fill",
                new Color(0.75f, 0.22f, 0.28f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            row.HpFill.raycastTarget = false;
            var fillRt = row.HpFill.rectTransform;
            fillRt.pivot = new Vector2(0f, 0.5f);

            row.HpLabel = UiFactory.AddText(_canvasRoot, $"P{index + 1}HpTxt", "100", 12,
                TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.285f, yMin + 0.03f), new Vector2(0.36f, yMax - 0.005f),
                Vector2.zero, Vector2.zero);

            // Cooldown indicators
            for (int s = 0; s < AbilityCaster.SlotCount; s++)
            {
                float x0 = 0.06f + s * 0.05f;
                var cdBg = UiFactory.AddPanel(_canvasRoot, $"P{index + 1}Cd{s}",
                    new Color(0.12f, 0.14f, 0.18f, 0.9f),
                    new Vector2(x0, yMin + 0.005f), new Vector2(x0 + 0.04f, yMin + 0.03f),
                    Vector2.zero, Vector2.zero);
                row.CdFills[s] = UiFactory.AddPanel(cdBg.transform, "Ready",
                    new Color(0.35f, 0.75f, 0.95f, 1f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            // Inventory strip (4 slots)
            for (int s = 0; s < 4; s++)
            {
                float x0 = 0.28f + s * 0.035f;
                UiFactory.AddPanel(_canvasRoot, $"P{index + 1}Inv{s}Bg",
                    new Color(0.1f, 0.1f, 0.12f, 0.75f),
                    new Vector2(x0, yMin + 0.005f), new Vector2(x0 + 0.03f, yMin + 0.03f),
                    Vector2.zero, Vector2.zero);
                row.InvSlots[s] = UiFactory.AddText(_canvasRoot, $"P{index + 1}Inv{s}", "·", 10,
                    TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.75f, 0.9f),
                    new Vector2(x0, yMin + 0.005f), new Vector2(x0 + 0.03f, yMin + 0.03f),
                    Vector2.zero, Vector2.zero);
            }
        }

        private static void TickRow(PlayerRow row)
        {
            if (row.Health == null) return;
            float pct = row.Health.MaxHealth > 0f
                ? Mathf.Clamp01(row.Health.CurrentHealth / row.Health.MaxHealth)
                : 0f;
            if (row.HpFill != null)
                row.HpFill.rectTransform.anchorMax = new Vector2(pct, 1f);
            if (row.HpLabel != null)
                row.HpLabel.text = $"{Mathf.CeilToInt(row.Health.CurrentHealth)}";

            if (row.Caster != null)
            {
                for (int s = 0; s < AbilityCaster.SlotCount; s++)
                {
                    if (row.CdFills[s] == null) continue;
                    float ready = 1f - row.Caster.GetCooldownNormalized(s);
                    row.CdFills[s].color = row.Caster.GetAbility(s) == null
                        ? new Color(0.2f, 0.2f, 0.22f, 0.5f)
                        : Color.Lerp(new Color(0.2f, 0.25f, 0.3f), new Color(0.35f, 0.75f, 0.95f), ready);
                    row.CdFills[s].rectTransform.anchorMax = new Vector2(1f, Mathf.Max(0.08f, ready));
                }
            }

            if (row.Inventory != null)
            {
                var items = row.Inventory.Items;
                for (int s = 0; s < 4; s++)
                {
                    if (row.InvSlots[s] == null) continue;
                    if (s < items.Count && items[s] != null)
                    {
                        string n = items[s].DisplayName;
                        row.InvSlots[s].text = string.IsNullOrEmpty(n) ? "•" : n.Substring(0, 1);
                    }
                    else row.InvSlots[s].text = "·";
                }
            }
        }

        private static Color PlayerColor(int index) => index switch
        {
            0 => new Color(0.75f, 0.45f, 1f),
            1 => new Color(0.35f, 0.85f, 0.45f),
            2 => new Color(0.95f, 0.85f, 0.3f),
            _ => new Color(0.95f, 0.35f, 0.35f)
        };
    }
}
