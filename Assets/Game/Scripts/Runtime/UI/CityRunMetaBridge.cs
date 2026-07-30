using RealmShards.Core;
using RealmShards.Rooms;
using RealmShards.Runs;
using RealmShards.Save;
using RealmShards.World;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Meta glue for CityRun: ensures world bootstrap exists, provides End Run controls,
    /// and ends the run when the sample encounter clears.
    /// Implements <see cref="ICityRunReady"/> so the full-screen stub UI is suppressed.
    /// </summary>
    public sealed class CityRunMetaBridge : MonoBehaviour, ICityRunReady
    {
        private bool _ending;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<CityRunMetaBridge>() != null)
            {
                return;
            }

            var go = new GameObject(nameof(CityRunMetaBridge));
            go.AddComponent<CityRunMetaBridge>();
        }

        private void Awake()
        {
            if (FindFirstObjectByType<CityRunBootstrap>() == null)
            {
                var bootstrap = new GameObject(nameof(CityRunBootstrap));
                bootstrap.AddComponent<CityRunBootstrap>();
            }
        }

        private void Start()
        {
            GameContext.EnsureEventSystem();
            BuildOverlay();
            Invoke(nameof(TryHookEncounter), 0.25f);
        }

        private void TryHookEncounter()
        {
            var room = FindFirstObjectByType<EncounterRoom>();
            if (room != null)
            {
                room.Cleared += OnEncounterCleared;
            }
        }

        private void OnDestroy()
        {
            var room = FindFirstObjectByType<EncounterRoom>();
            if (room != null)
            {
                room.Cleared -= OnEncounterCleared;
            }
        }

        private void OnEncounterCleared(EncounterRoom room)
        {
            // Auto-complete run on first room clear (Stage 2 sample).
            End(success: true, vestiges: 25, summary: "Encounter cleared.");
        }

        private void BuildOverlay()
        {
            var canvas = UiFactory.CreateScreenCanvas("CityRunMetaHUD", 200);
            canvas.transform.SetParent(transform, false);

            UiFactory.AddText(canvas.transform, "Hint", "CityRun — clear the room or use buttons", 18,
                TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f),
                new Vector2(0.2f, 0.92f), new Vector2(0.8f, 0.99f), Vector2.zero, Vector2.zero);

            var win = UiFactory.AddButton(canvas.transform, "Win", "End: Win",
                new Vector2(0.02f, 0.02f), new Vector2(0.18f, 0.08f), Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.4f, 0.25f, 0.9f));
            win.onClick.AddListener(() => End(true, 25, "Manual win."));

            var fail = UiFactory.AddButton(canvas.transform, "Fail", "End: Fail",
                new Vector2(0.20f, 0.02f), new Vector2(0.36f, 0.08f), Vector2.zero, Vector2.zero,
                new Color(0.45f, 0.15f, 0.15f, 0.9f));
            fail.onClick.AddListener(() => End(false, 0, "Manual failure."));
        }

        private void End(bool success, int vestiges, string summary)
        {
            if (_ending)
            {
                return;
            }

            _ending = true;
            var ctx = GameContext.Instance;
            if (ctx == null)
            {
                return;
            }

            var s = ctx.RunSession;
            var city = s?.CityId ?? ContentIdDefaults.CityStarter;
            var route = s?.RouteId ?? ContentIdDefaults.RouteStarterMain;
            var outcome = success
                ? RunOutcome.Success(city, route, vestiges, summary)
                : RunOutcome.Failure(city, route, summary);
            ctx.Runs.EndRun(outcome);
        }
    }
}
