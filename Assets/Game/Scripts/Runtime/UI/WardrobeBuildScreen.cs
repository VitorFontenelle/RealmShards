using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Shared wardrobe builds: six slots for the whole lobby party.
    /// </summary>
    public sealed class WardrobeBuildScreen : MonoBehaviour
    {
        private GameObject _root;
        private Text _title;
        private Text _status;
        private readonly List<Text> _slotSummaries = new List<Text>();
        private ConfirmDialog _confirm;
        private int _playerIndex;

        public event System.Action Closed;

        public bool IsVisible => _root != null && _root.activeSelf;

        public static WardrobeBuildScreen EnsurePresent(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<WardrobeBuildScreen>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(WardrobeBuildScreen));
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<WardrobeBuildScreen>();
            screen.Build();
            screen.Hide();
            return screen;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("WardrobeUI", 370);
            canvas.transform.SetParent(transform, false);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            _root = canvas.gameObject;

            UiFactory.AddPanel(canvas.transform, "Dim", new Color(0.02f, 0.03f, 0.06f, 0.65f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(canvas.transform, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.12f, 0.06f);
            boxRt.anchorMax = new Vector2(0.88f, 0.92f);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;

            UiFactory.AddPanel(box.transform, "Border", new Color(0.9f, 0.88f, 0.95f, 0.95f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.AddPanel(box.transform, "Background", new Color(0.08f, 0.07f, 0.12f, 0.96f),
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            _title = UiFactory.AddText(box.transform, "Title", "WARDROBE", 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            _status = UiFactory.AddText(box.transform, "Status", string.Empty, 14, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.78f, 0.85f, 1f),
                new Vector2(0.05f, 0.03f), new Vector2(0.58f, 0.09f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            float y = 0.88f;
            for (int slot = 0; slot < PlayerBuildService.SlotCount; slot++)
                AddSlotRow(box.transform, slot, ref y);

            var close = UiFactory.AddButton(box.transform, "Close", "DONE",
                new Vector2(0.62f, 0.03f), new Vector2(0.94f, 0.09f), Vector2.zero, Vector2.zero,
                new Color(0.18f, 0.2f, 0.26f, 0.95f), UiFonts.MenuBold);
            close.GetComponentInChildren<Text>().fontSize = 18;
            close.onClick.AddListener(Hide);

            _confirm = ConfirmDialog.EnsurePresent(transform);
        }

        private void AddSlotRow(Transform parent, int slotIndex, ref float yTop)
        {
            float yMin = yTop - 0.12f;

            UiFactory.AddText(parent, $"Slot{slotIndex}Label", $"SLOT {slotIndex + 1}", 15, TextAnchor.UpperLeft,
                new Color(0.82f, 0.78f, 0.95f, 1f),
                new Vector2(0.05f, yMin + 0.03f), new Vector2(0.18f, yTop), Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            var summary = UiFactory.AddText(parent, $"Slot{slotIndex}Summary", "(Empty)", 13, TextAnchor.UpperLeft,
                Color.white, new Vector2(0.18f, yMin), new Vector2(0.56f, yTop), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);
            _slotSummaries.Add(summary);

            int captured = slotIndex;
            var save = UiFactory.AddButton(parent, $"Save{slotIndex}", "SAVE",
                new Vector2(0.58f, yMin), new Vector2(0.7f, yTop), Vector2.zero, Vector2.zero,
                new Color(0.18f, 0.28f, 0.38f, 0.95f), UiFonts.MenuBold);
            save.GetComponentInChildren<Text>().fontSize = 13;
            save.onClick.AddListener(() => PromptSave(captured));

            var dress = UiFactory.AddButton(parent, $"Dress{slotIndex}", "DRESS",
                new Vector2(0.71f, yMin), new Vector2(0.83f, yTop), Vector2.zero, Vector2.zero,
                new Color(0.22f, 0.18f, 0.34f, 0.95f), UiFonts.MenuBold);
            dress.GetComponentInChildren<Text>().fontSize = 13;
            dress.onClick.AddListener(() => PromptDress(captured));

            var delete = UiFactory.AddButton(parent, $"Delete{slotIndex}", "DELETE",
                new Vector2(0.84f, yMin), new Vector2(0.96f, yTop), Vector2.zero, Vector2.zero,
                new Color(0.34f, 0.14f, 0.16f, 0.95f), UiFonts.MenuBold);
            delete.GetComponentInChildren<Text>().fontSize = 13;
            delete.onClick.AddListener(() => PromptDelete(captured));

            yTop = yMin - 0.01f;
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
            _confirm?.Hide();
            if (_root != null)
                _root.SetActive(false);
            Closed?.Invoke();
        }

        private void PromptSave(int slotIndex)
        {
            _confirm.Show(
                $"Save P{_playerIndex + 1}'s current spells and item to shared Slot {slotIndex + 1}? This overwrites that slot for everyone.",
                () =>
                {
                    var ctx = GameContext.Instance;
                    if (ctx?.Save == null) return;
                    PlayerBuildService.SaveCurrentBuild(_playerIndex, slotIndex, ctx.Save);
                    Refresh();
                });
        }

        private void PromptDress(int slotIndex)
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;

            var preset = PlayerBuildService.GetPreset(ctx.Save.Current.meta, slotIndex);
            if (preset.IsEmpty)
            {
                _status.text = $"Slot {slotIndex + 1} is empty.";
                return;
            }

            _confirm.Show(
                $"Equip shared Slot {slotIndex + 1} for all players? Everyone's lobby loadout will be replaced.",
                () =>
                {
                    PlayerBuildService.DressBuild(slotIndex, ctx.Save);
                    Refresh();
                });
        }

        private void PromptDelete(int slotIndex)
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;

            var preset = PlayerBuildService.GetPreset(ctx.Save.Current.meta, slotIndex);
            if (preset.IsEmpty)
            {
                _status.text = $"Slot {slotIndex + 1} is already empty.";
                return;
            }

            _confirm.Show(
                $"Delete shared Slot {slotIndex + 1} for everyone? This cannot be undone.",
                () =>
                {
                    PlayerBuildService.DeleteBuild(slotIndex, ctx.Save);
                    Refresh();
                });
        }

        private void Refresh()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;

            var meta = ctx.Save.Current.meta;
            _title.text = "WARDROBE — SHARED BUILDS";
            _status.text = "Six shared slots · Save uses your loadout · Dress applies to all players.";

            for (int i = 0; i < _slotSummaries.Count && i < PlayerBuildService.SlotCount; i++)
            {
                var preset = PlayerBuildService.GetPreset(meta, i);
                _slotSummaries[i].text = PlayerBuildService.DescribePreset(meta, preset);
            }
        }
    }
}
