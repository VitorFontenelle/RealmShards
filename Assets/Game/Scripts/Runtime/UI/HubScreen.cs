using RealmShards.Core;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Placeholder Hub: Start → lobby with 1-4 local players, loadout stubs, Start Run → CityRun.
    /// </summary>
    public sealed class HubScreen : MonoBehaviour
    {
        private GameObject _titlePanel;
        private GameObject _lobbyPanel;
        private Text _statusText;
        private Text _loadoutText;
        private int _playerCount = 1;
        private Text[] _slotLabels;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<HubScreen>() != null)
            {
                return;
            }

            var canvas = UiFactory.CreateScreenCanvas("HubUI");
            canvas.gameObject.AddComponent<HubScreen>();
        }

        private void Start()
        {
            Build();
            ShowTitle();
            Refresh();
        }

        private void Build()
        {
            var root = transform;

            UiFactory.AddPanel(root, "Background", new Color(0.08f, 0.09f, 0.12f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            _titlePanel = new GameObject("TitlePanel", typeof(RectTransform));
            _titlePanel.transform.SetParent(root, false);
            StretchFull(_titlePanel.GetComponent<RectTransform>());

            UiFactory.AddText(_titlePanel.transform, "Title", "RealmShards", 72, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.9f, 1f),
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f), Vector2.zero, Vector2.zero);

            UiFactory.AddText(_titlePanel.transform, "Tagline", "Local hub — placeholder UI", 24, TextAnchor.MiddleCenter,
                new Color(0.65f, 0.7f, 0.78f),
                new Vector2(0.2f, 0.48f), new Vector2(0.8f, 0.56f), Vector2.zero, Vector2.zero);

            var start = UiFactory.AddButton(_titlePanel.transform, "Start", "Start",
                new Vector2(0.35f, 0.28f), new Vector2(0.65f, 0.40f), Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.45f, 0.32f, 1f));
            start.onClick.AddListener(ShowLobby);

            _lobbyPanel = new GameObject("LobbyPanel", typeof(RectTransform));
            _lobbyPanel.transform.SetParent(root, false);
            StretchFull(_lobbyPanel.GetComponent<RectTransform>());

            UiFactory.AddText(_lobbyPanel.transform, "LobbyTitle", "Hub Lobby", 48, TextAnchor.MiddleCenter,
                new Color(0.85f, 0.9f, 1f),
                new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f), Vector2.zero, Vector2.zero);

            _statusText = UiFactory.AddText(_lobbyPanel.transform, "Status", string.Empty, 22, TextAnchor.UpperLeft, Color.white,
                new Vector2(0.08f, 0.52f), new Vector2(0.48f, 0.84f), Vector2.zero, Vector2.zero);

            UiFactory.AddText(_lobbyPanel.transform, "PlayersHeader", "Join players (local)", 26, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0.52f, 0.78f), new Vector2(0.92f, 0.84f), Vector2.zero, Vector2.zero);

            _slotLabels = new Text[4];
            for (var i = 0; i < 4; i++)
            {
                var index = i;
                var yMax = 0.76f - i * 0.08f;
                var yMin = yMax - 0.07f;
                var button = UiFactory.AddButton(_lobbyPanel.transform, $"Slot{i}", $"Slot {i + 1}",
                    new Vector2(0.52f, yMin), new Vector2(0.92f, yMax), Vector2.zero, Vector2.zero,
                    new Color(0.16f, 0.2f, 0.28f, 1f));
                _slotLabels[i] = button.GetComponentInChildren<Text>();
                button.onClick.AddListener(() => SetPlayerCount(index + 1));
            }

            _loadoutText = UiFactory.AddText(_lobbyPanel.transform, "Loadout", string.Empty, 20, TextAnchor.UpperLeft,
                new Color(0.8f, 0.85f, 0.9f),
                new Vector2(0.08f, 0.28f), new Vector2(0.48f, 0.50f), Vector2.zero, Vector2.zero);

            var startRun = UiFactory.AddButton(_lobbyPanel.transform, "StartRun", "Start Run",
                new Vector2(0.55f, 0.10f), new Vector2(0.92f, 0.20f), Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.45f, 0.32f, 1f));
            startRun.onClick.AddListener(OnStartRun);

            var saveBtn = UiFactory.AddButton(_lobbyPanel.transform, "SaveNow", "Save",
                new Vector2(0.08f, 0.10f), new Vector2(0.28f, 0.18f), Vector2.zero, Vector2.zero);
            saveBtn.onClick.AddListener(OnSave);

            if (GameContext.Instance != null)
            {
                _playerCount = Mathf.Clamp(GameContext.Instance.Save.Current.settings.localPlayerCount, 1, 4);
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ShowTitle()
        {
            _titlePanel.SetActive(true);
            _lobbyPanel.SetActive(false);
        }

        private void ShowLobby()
        {
            _titlePanel.SetActive(false);
            _lobbyPanel.SetActive(true);
            Refresh();
        }

        private void SetPlayerCount(int count)
        {
            _playerCount = Mathf.Clamp(count, 1, 4);
            if (GameContext.Instance != null)
            {
                GameContext.Instance.Save.Current.settings.localPlayerCount = _playerCount;
            }

            RefreshSlots();
        }

        private void Refresh()
        {
            var ctx = GameContext.Instance;
            if (ctx == null || _statusText == null)
            {
                return;
            }

            var meta = ctx.Save.Current.meta;
            var cityName = ctx.Content != null
                ? ctx.Content.GetDisplayName(meta.selectedCityId, meta.selectedCityId)
                : meta.selectedCityId;

            _statusText.text =
                $"Year: {meta.year}\n" +
                $"Decade: {meta.decade}\n" +
                $"Arcane Vestiges: {meta.arcaneVestiges}\n" +
                $"City: {cityName}\n" +
                $"Save: {ctx.Save.SaveFilePath}";

            var loadout = "Loadout (placeholders)\n";
            for (var i = 0; i < meta.equippedAbilityIds.Count; i++)
            {
                var id = meta.equippedAbilityIds[i];
                var label = string.IsNullOrEmpty(id)
                    ? "(empty)"
                    : ctx.Content.GetDisplayName(id, id);
                loadout += $"  [{i + 1}] {label}\n";
            }

            loadout += "\nTODO (combat): bind abilities to Player prefab.";
            _loadoutText.text = loadout;
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            if (_slotLabels == null)
            {
                return;
            }

            for (var i = 0; i < _slotLabels.Length; i++)
            {
                var active = i < _playerCount;
                _slotLabels[i].text = active
                    ? $"Slot {i + 1} — Joined (P{i + 1})"
                    : $"Slot {i + 1} — Empty (click to set count)";
            }
        }

        private void OnStartRun()
        {
            var ctx = GameContext.Instance;
            if (ctx == null)
            {
                return;
            }

            var meta = ctx.Save.Current.meta;
            ctx.Runs.BeginRun(meta.selectedCityId, ContentIdDefaults.RouteStarterMain, _playerCount);
        }

        private void OnSave()
        {
            GameContext.Instance?.Save.Save();
            Refresh();
        }
    }
}
