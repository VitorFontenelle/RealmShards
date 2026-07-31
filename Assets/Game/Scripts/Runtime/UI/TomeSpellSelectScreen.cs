using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Per-player spell selection from the lobby tome.
    /// </summary>
    public sealed class TomeSpellSelectScreen : MonoBehaviour
    {
        private GameObject _root;
        private Text _title;
        private Text _status;
        private int _playerIndex;
        private readonly Dictionary<AbilitySlotRole, Text> _valueLabels = new Dictionary<AbilitySlotRole, Text>();

        public static TomeSpellSelectScreen EnsurePresent(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<TomeSpellSelectScreen>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(TomeSpellSelectScreen));
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<TomeSpellSelectScreen>();
            screen.Build();
            screen.Hide();
            return screen;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("TomeSpellUI", 350);
            canvas.transform.SetParent(transform, false);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            _root = canvas.gameObject;

            UiFactory.AddPanel(canvas.transform, "Dim", new Color(0.02f, 0.03f, 0.06f, 0.65f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(canvas.transform, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.22f, 0.18f);
            boxRt.anchorMax = new Vector2(0.78f, 0.82f);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;

            UiFactory.AddPanel(box.transform, "Border", new Color(0.9f, 0.88f, 0.95f, 0.95f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.AddPanel(box.transform, "Background", new Color(0.08f, 0.07f, 0.12f, 0.96f),
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            _title = UiFactory.AddText(box.transform, "Title", "SPELL TOME", 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            float y = 0.78f;
            AddRoleRow(box.transform, AbilitySlotRole.Primary, "PRIMARY ATTACK", ref y);
            AddRoleRow(box.transform, AbilitySlotRole.Dash, "DASH", ref y);
            AddRoleRow(box.transform, AbilitySlotRole.Signature, "SIGNATURE", ref y);
            AddRoleRow(box.transform, AbilitySlotRole.Ultimate, "ULTIMATE", ref y);

            _status = UiFactory.AddText(box.transform, "Status", string.Empty, 16, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.78f, 0.85f, 1f),
                new Vector2(0.08f, 0.06f), new Vector2(0.62f, 0.14f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            var close = UiFactory.AddButton(box.transform, "Close", "DONE",
                new Vector2(0.66f, 0.06f), new Vector2(0.92f, 0.14f), Vector2.zero, Vector2.zero,
                new Color(0.18f, 0.2f, 0.26f, 0.95f), UiFonts.MenuBold);
            close.GetComponentInChildren<Text>().fontSize = 20;
            close.onClick.AddListener(Hide);
        }

        private void AddRoleRow(Transform parent, AbilitySlotRole role, string label, ref float yTop)
        {
            float yMin = yTop - 0.14f;
            UiFactory.AddText(parent, $"{role}Label", label, 18, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.08f, yMin), new Vector2(0.42f, yTop), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            var value = UiFactory.AddText(parent, $"{role}Value", "-", 18, TextAnchor.MiddleRight, Color.white,
                new Vector2(0.42f, yMin), new Vector2(0.78f, yTop), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);
            _valueLabels[role] = value;

            var prev = UiFactory.AddButton(parent, $"{role}Prev", "<",
                new Vector2(0.80f, yMin), new Vector2(0.88f, yTop), Vector2.zero, Vector2.zero,
                new Color(0.16f, 0.18f, 0.24f, 0.9f), UiFonts.MenuBold);
            prev.GetComponentInChildren<Text>().fontSize = 18;
            prev.onClick.AddListener(() => Cycle(role, -1));

            var next = UiFactory.AddButton(parent, $"{role}Next", ">",
                new Vector2(0.88f, yMin), new Vector2(0.96f, yTop), Vector2.zero, Vector2.zero,
                new Color(0.16f, 0.18f, 0.24f, 0.9f), UiFonts.MenuBold);
            next.GetComponentInChildren<Text>().fontSize = 18;
            next.onClick.AddListener(() => Cycle(role, 1));

            yTop = yMin - 0.02f;
        }

        public void ShowForPlayer(int playerIndex)
        {
            _playerIndex = Mathf.Clamp(playerIndex, 0, 3);
            _root.SetActive(true);
            gameObject.SetActive(true);
            GameContext.EnsureEventSystem();
            Refresh();
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Cycle(AbilitySlotRole role, int delta)
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;

            var meta = ctx.Save.Current.meta;
            if (role == AbilitySlotRole.Ultimate && !PlayerLoadoutService.UltimateSlotUnlocked(meta))
            {
                _status.text = "Ultimate slot locked.";
                return;
            }

            var options = new List<string> { string.Empty };
            foreach (var id in PlayerLoadoutService.CandidatesForRole(meta, role))
            {
                if (!options.Contains(id))
                    options.Add(id);
            }

            var loadout = PlayerLoadoutService.GetLoadout(meta, _playerIndex);
            string current = loadout.GetAbilityId(role) ?? string.Empty;
            int idx = options.IndexOf(current);
            if (idx < 0) idx = 0;
            idx = (idx + delta + options.Count) % options.Count;
            PlayerLoadoutService.SetAbility(_playerIndex, role, options[idx], ctx.Save);
            Refresh();
        }

        private void Refresh()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;
            var meta = ctx.Save.Current.meta;
            var loadout = PlayerLoadoutService.GetLoadout(meta, _playerIndex);
            _title.text = $"SPELL TOME — P{_playerIndex + 1}";

            foreach (var pair in _valueLabels)
            {
                if (pair.Key == AbilitySlotRole.Ultimate && !PlayerLoadoutService.UltimateSlotUnlocked(meta))
                {
                    pair.Value.text = "(Locked)";
                    continue;
                }

                string id = loadout.GetAbilityId(pair.Key);
                pair.Value.text = string.IsNullOrEmpty(id)
                    ? "(Empty)"
                    : ctx.Content.GetDisplayName(id, id);
            }

            _status.text = "Signature starts at Signature tier · Ultimate starts at Ultimate tier.";
        }
    }
}
