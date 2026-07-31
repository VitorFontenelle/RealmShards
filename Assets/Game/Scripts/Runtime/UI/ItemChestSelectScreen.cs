using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Per-player lobby item chest: pick category, then one unlocked item for the run.
    /// </summary>
    public sealed class ItemChestSelectScreen : MonoBehaviour
    {
        private GameObject _root;
        private GameObject _categoryPanel;
        private GameObject _itemPanel;
        private RectTransform _itemList;
        private Text _title;
        private Text _status;
        private Text _currentSelection;
        private int _playerIndex;
        private ItemCategory? _activeCategory;

        public event System.Action Closed;

        public bool IsVisible => _root != null && _root.activeSelf;

        public static ItemChestSelectScreen EnsurePresent(Transform parent)
        {
            var existing = Object.FindFirstObjectByType<ItemChestSelectScreen>();
            if (existing != null)
                return existing;

            var go = new GameObject(nameof(ItemChestSelectScreen));
            go.transform.SetParent(parent, false);
            var screen = go.AddComponent<ItemChestSelectScreen>();
            screen.Build();
            screen.Hide();
            return screen;
        }

        private void Build()
        {
            var canvas = UiFactory.CreateScreenCanvas("ItemChestUI", 360);
            canvas.transform.SetParent(transform, false);
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            _root = canvas.gameObject;

            UiFactory.AddPanel(canvas.transform, "Dim", new Color(0.02f, 0.03f, 0.06f, 0.65f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var box = new GameObject("Box", typeof(RectTransform));
            box.transform.SetParent(canvas.transform, false);
            var boxRt = box.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.2f, 0.16f);
            boxRt.anchorMax = new Vector2(0.8f, 0.84f);
            boxRt.offsetMin = Vector2.zero;
            boxRt.offsetMax = Vector2.zero;

            UiFactory.AddPanel(box.transform, "Border", new Color(0.9f, 0.88f, 0.95f, 0.95f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UiFactory.AddPanel(box.transform, "Background", new Color(0.08f, 0.07f, 0.12f, 0.96f),
                Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            _title = UiFactory.AddText(box.transform, "Title", "ITEM CHEST", 30, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), Vector2.zero, Vector2.zero, UiFonts.MenuBold);

            _currentSelection = UiFactory.AddText(box.transform, "Current", "Selected: (none)", 18, TextAnchor.MiddleCenter,
                new Color(0.82f, 0.78f, 0.95f, 1f),
                new Vector2(0.08f, 0.8f), new Vector2(0.92f, 0.87f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            _status = UiFactory.AddText(box.transform, "Status", string.Empty, 16, TextAnchor.MiddleCenter,
                new Color(0.75f, 0.78f, 0.85f, 1f),
                new Vector2(0.08f, 0.06f), new Vector2(0.62f, 0.13f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);

            var close = UiFactory.AddButton(box.transform, "Close", "DONE",
                new Vector2(0.66f, 0.06f), new Vector2(0.92f, 0.13f), Vector2.zero, Vector2.zero,
                new Color(0.18f, 0.2f, 0.26f, 0.95f), UiFonts.MenuBold);
            close.GetComponentInChildren<Text>().fontSize = 20;
            close.onClick.AddListener(Hide);

            _categoryPanel = new GameObject("CategoryPanel", typeof(RectTransform));
            _categoryPanel.transform.SetParent(box.transform, false);
            StretchFull(_categoryPanel.GetComponent<RectTransform>());

            float y = 0.72f;
            AddCategoryButton(_categoryPanel.transform, ItemCategory.Attack, ref y);
            AddCategoryButton(_categoryPanel.transform, ItemCategory.Defense, ref y);
            AddCategoryButton(_categoryPanel.transform, ItemCategory.Misc, ref y);

            _itemPanel = new GameObject("ItemPanel", typeof(RectTransform));
            _itemPanel.transform.SetParent(box.transform, false);
            StretchFull(_itemPanel.GetComponent<RectTransform>());
            _itemPanel.SetActive(false);

            var listGo = new GameObject("ItemList", typeof(RectTransform));
            listGo.transform.SetParent(_itemPanel.transform, false);
            _itemList = listGo.GetComponent<RectTransform>();
            _itemList.anchorMin = new Vector2(0.08f, 0.18f);
            _itemList.anchorMax = new Vector2(0.92f, 0.78f);
            _itemList.offsetMin = Vector2.zero;
            _itemList.offsetMax = Vector2.zero;

            var back = UiFactory.AddButton(_itemPanel.transform, "Back", "BACK",
                new Vector2(0.08f, 0.06f), new Vector2(0.32f, 0.13f), Vector2.zero, Vector2.zero,
                new Color(0.16f, 0.18f, 0.24f, 0.95f), UiFonts.MenuBold);
            back.GetComponentInChildren<Text>().fontSize = 18;
            back.onClick.AddListener(ShowCategories);
        }

        private void AddCategoryButton(Transform parent, ItemCategory category, ref float yTop)
        {
            float yMin = yTop - 0.12f;
            var btn = UiFactory.AddButton(parent, $"{category}Btn", PlayerItemLoadoutService.CategoryLabel(category),
                new Vector2(0.18f, yMin), new Vector2(0.82f, yTop), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.16f, 0.28f, 0.95f), UiFonts.MenuBold);
            btn.GetComponentInChildren<Text>().fontSize = 22;
            btn.onClick.AddListener(() => ShowItems(category));
            yTop = yMin - 0.04f;
        }

        public void ShowForPlayer(int playerIndex)
        {
            _playerIndex = Mathf.Clamp(playerIndex, 0, 3);
            _root.SetActive(true);
            gameObject.SetActive(true);
            GameContext.EnsureEventSystem();
            ShowCategories();
            Refresh();
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
            _activeCategory = null;
            Closed?.Invoke();
        }

        private void ShowCategories()
        {
            _activeCategory = null;
            if (_categoryPanel != null) _categoryPanel.SetActive(true);
            if (_itemPanel != null) _itemPanel.SetActive(false);
            Refresh();
        }

        private void ShowItems(ItemCategory category)
        {
            _activeCategory = category;
            if (_categoryPanel != null) _categoryPanel.SetActive(false);
            if (_itemPanel != null) _itemPanel.SetActive(true);
            RebuildItemList(category);
            Refresh();
        }

        private void RebuildItemList(ItemCategory category)
        {
            for (int i = _itemList.childCount - 1; i >= 0; i--)
                Destroy(_itemList.GetChild(i).gameObject);

            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;

            var options = new List<string>();
            foreach (var id in PlayerItemLoadoutService.CandidatesForCategory(ctx.Save.Current.meta, category))
            {
                if (!options.Contains(id))
                    options.Add(id);
            }

            float yTop = 1f;
            const float rowHeight = 0.14f;
            const float gap = 0.02f;

            AddItemRow("(None)", string.Empty, ref yTop, rowHeight, gap);

            if (options.Count == 0)
            {
                var empty = UiFactory.AddText(_itemList, "Empty", "No unlocked items in this category yet.",
                    18, TextAnchor.MiddleCenter, new Color(0.75f, 0.75f, 0.8f, 1f),
                    new Vector2(0f, 0.35f), new Vector2(1f, 0.55f), Vector2.zero, Vector2.zero, UiFonts.MenuRegular);
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                string id = options[i];
                var def = ItemCatalog.Get(id);
                string label = def != null ? def.DisplayName : id;
                AddItemRow(label, id, ref yTop, rowHeight, gap);
            }
        }

        private void AddItemRow(string label, string itemId, ref float yTop, float rowHeight, float gap)
        {
            float yMin = yTop - rowHeight;
            string current = PlayerItemLoadoutService.GetSelectedItem(GameContext.Instance.Save.Current.meta, _playerIndex);
            bool selected = string.IsNullOrEmpty(itemId)
                ? string.IsNullOrEmpty(current)
                : current == itemId;

            var color = selected
                ? new Color(0.35f, 0.22f, 0.5f, 0.98f)
                : new Color(0.16f, 0.18f, 0.24f, 0.92f);

            var btn = UiFactory.AddButton(_itemList, $"Item_{itemId}_{label}", label,
                new Vector2(0f, yMin), new Vector2(1f, yTop), Vector2.zero, Vector2.zero, color, UiFonts.MenuRegular);
            btn.GetComponentInChildren<Text>().fontSize = 18;
            string captured = itemId;
            btn.onClick.AddListener(() => SelectItem(captured));

            yTop = yMin - gap;
        }

        private void SelectItem(string itemId)
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save == null) return;

            PlayerItemLoadoutService.SetSelectedItem(_playerIndex, itemId, ctx.Save);
            if (_activeCategory.HasValue)
                RebuildItemList(_activeCategory.Value);
            Refresh();
        }

        private void Refresh()
        {
            var ctx = GameContext.Instance;
            if (ctx?.Save?.Current?.meta == null) return;

            _title.text = _activeCategory.HasValue
                ? $"ITEM CHEST — P{_playerIndex + 1} · {PlayerItemLoadoutService.CategoryLabel(_activeCategory.Value)}"
                : $"ITEM CHEST — P{_playerIndex + 1}";

            string selectedId = PlayerItemLoadoutService.GetSelectedItem(ctx.Save.Current.meta, _playerIndex);
            if (string.IsNullOrEmpty(selectedId))
            {
                _currentSelection.text = "Selected: (none)";
            }
            else
            {
                var def = ItemCatalog.Get(selectedId);
                _currentSelection.text = $"Selected: {def?.DisplayName ?? selectedId}";
            }

            _status.text = "Choose one item for this run. Selecting another replaces your pick.";
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
