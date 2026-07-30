using RealmShards.Core;
using RealmShards.Rooms;
using RealmShards.Runs;
using RealmShards.Save;
using RealmShards.World;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Meta glue for CityRun: bootstrap, End Run controls, advance route after clear.
    /// </summary>
    public sealed class CityRunMetaBridge : MonoBehaviour, ICityRunReady
    {
        private bool _ending;
        private bool _waitingCore;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<CityRunMetaBridge>() != null)
                return;

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
                room.Cleared += OnEncounterCleared;
        }

        private void OnDestroy()
        {
            var room = FindFirstObjectByType<EncounterRoom>();
            if (room != null)
                room.Cleared -= OnEncounterCleared;
        }

        private void OnEncounterCleared(EncounterRoom room)
        {
            var session = GameContext.Instance?.RunSession;
            if (session != null && session.AwaitingArcaneCore)
            {
                _waitingCore = true;
                Invoke(nameof(FinishAfterCoreOrTimeout), 12f);
                return;
            }

            // Give a short window for Arcane Core pickup if champion just died.
            Invoke(nameof(AdvanceOrEnd), 1.5f);
        }

        private void FinishAfterCoreOrTimeout()
        {
            if (_ending) return;
            AdvanceOrEnd();
        }

        private void AdvanceOrEnd()
        {
            if (_ending) return;
            var ctx = GameContext.Instance;
            if (ctx == null) return;

            var session = ctx.RunSession;
            if (session != null && session.AwaitingArcaneCore)
            {
                // Still in UI — wait a bit more
                if (_waitingCore)
                    return;
            }

            if (session != null && session.HasNextNode)
            {
                _ending = true;
                ctx.Runs.AdvanceToNextCity();
                return;
            }

            End(success: true, vestiges: session != null && session.IsCapitalNode ? 40 : 25,
                summary: session != null && session.IsCapitalNode
                    ? "Capital secured — route complete."
                    : "City secured.");
        }

        private void BuildOverlay()
        {
            var canvas = UiFactory.CreateScreenCanvas("CityRunMetaHUD", 200);
            UiScaleConfig.Apply(canvas.GetComponent<UnityEngine.UI.CanvasScaler>());
            canvas.transform.SetParent(transform, false);

            var city = GameContext.Instance?.RunSession?.CityId ?? "city";
            var node = GameContext.Instance?.RunSession?.WorldNodeIndex ?? 0;
            UiFactory.AddText(canvas.transform, "Hint",
                $"CityRun — {city} (node {node + 1}) · clear room or End buttons",
                16, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f),
                new Vector2(0.1f, 0.92f), new Vector2(0.9f, 0.99f), Vector2.zero, Vector2.zero);

            var win = UiFactory.AddButton(canvas.transform, "Win", "End: Win",
                new Vector2(0.02f, 0.02f), new Vector2(0.18f, 0.08f), Vector2.zero, Vector2.zero,
                new Color(0.12f, 0.4f, 0.25f, 0.9f));
            win.onClick.AddListener(() => End(true, 25, "Manual win."));

            var fail = UiFactory.AddButton(canvas.transform, "Fail", "End: Fail",
                new Vector2(0.20f, 0.02f), new Vector2(0.36f, 0.08f), Vector2.zero, Vector2.zero,
                new Color(0.45f, 0.15f, 0.15f, 0.9f));
            fail.onClick.AddListener(() => End(false, 0, "Manual failure."));

            var next = UiFactory.AddButton(canvas.transform, "Next", "Next City",
                new Vector2(0.38f, 0.02f), new Vector2(0.56f, 0.08f), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.3f, 0.45f, 0.9f));
            next.onClick.AddListener(() =>
            {
                if (_ending) return;
                _ending = true;
                GameContext.Instance?.Runs.AdvanceToNextCity();
            });
        }

        private void End(bool success, int vestiges, string summary)
        {
            if (_ending) return;
            _ending = true;
            var ctx = GameContext.Instance;
            if (ctx == null) return;

            var s = ctx.RunSession;
            var city = s?.CityId ?? ContentIdDefaults.CityStarter;
            var route = s?.RouteId ?? ContentIdDefaults.RouteWorldMain;
            var outcome = success
                ? RunOutcome.Success(city, route, vestiges, summary)
                : RunOutcome.Failure(city, route, summary);
            ctx.Runs.EndRun(outcome);
        }
    }
}
