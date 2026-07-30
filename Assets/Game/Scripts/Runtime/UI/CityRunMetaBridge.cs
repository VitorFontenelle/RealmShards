using RealmShards.Combat;
using RealmShards.Core;
using RealmShards.Rooms;
using RealmShards.Runs;
using RealmShards.Save;
using RealmShards.World;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Meta glue for CityRun: End Run controls, advance route after full city clear (not first room).
    /// </summary>
    public sealed class CityRunMetaBridge : MonoBehaviour, ICityRunReady
    {
        private bool _ending;
        private bool _waitingCore;
        private bool _cityComplete;
        private CityRunDirector _director;
        private Text _roomHint;

        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<CityRunMetaBridge>() != null)
                return;

            var go = new GameObject(nameof(CityRunMetaBridge));
            go.AddComponent<CityRunMetaBridge>();
        }

        private void Awake()
        {
            HitStop.EnsureRunningTimeScale();
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
            Invoke(nameof(TryHookDirector), 0.3f);
        }

        private void Update()
        {
            if (!_cityComplete || _ending || !_waitingCore) return;
            var session = GameContext.Instance?.RunSession;
            if (session != null && !session.AwaitingArcaneCore)
            {
                _waitingCore = false;
                CancelInvoke(nameof(FinishAfterCoreOrTimeout));
                AdvanceOrEnd();
            }
        }

        private void TryHookDirector()
        {
            _director = FindFirstObjectByType<CityRunDirector>();
            if (_director == null)
            {
                // Legacy fallback: single room still must not end whole route immediately —
                // only advance after clear if it's treated as city complete.
                var room = FindFirstObjectByType<EncounterRoom>();
                if (room != null)
                    room.Cleared += OnLegacyRoomCleared;
                return;
            }

            _director.CityCompleted += OnCityCompleted;
            _director.RoomStarted += OnRoomStarted;
            OnRoomStarted(_director.RoomIndex, _director.TotalRooms);
        }

        private void OnDestroy()
        {
            if (_director != null)
            {
                _director.CityCompleted -= OnCityCompleted;
                _director.RoomStarted -= OnRoomStarted;
            }

            var room = FindFirstObjectByType<EncounterRoom>();
            if (room != null)
                room.Cleared -= OnLegacyRoomCleared;

            HitStop.EnsureRunningTimeScale();
        }

        private void OnRoomStarted(int index, int total)
        {
            if (_roomHint != null)
            {
                var city = GameContext.Instance?.RunSession?.CityId ?? "city";
                string kind = index >= total - 1 ? "Champion" : "Room";
                _roomHint.text =
                    $"CityRun — {city} · {kind} {index + 1}/{total} · clear all rooms before route advance";
            }
        }

        private void OnCityCompleted()
        {
            _cityComplete = true;
            var session = GameContext.Instance?.RunSession;
            if (session != null && session.AwaitingArcaneCore)
            {
                _waitingCore = true;
                Invoke(nameof(FinishAfterCoreOrTimeout), 14f);
                return;
            }

            Invoke(nameof(AdvanceOrEnd), 1.5f);
        }

        private void OnLegacyRoomCleared(EncounterRoom room)
        {
            // Without director, treat single clear as city complete (old scenes).
            OnCityCompleted();
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
            if (session != null && session.AwaitingArcaneCore && _waitingCore)
            {
                // Still in Arcane Core UI — keep waiting until unlock screen clears flag or timeout already fired.
                // Soft proceed after timeout path.
            }

            HitStop.EnsureRunningTimeScale();

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
            UiScaleConfig.Apply(canvas.GetComponent<CanvasScaler>());
            canvas.transform.SetParent(transform, false);

            var city = GameContext.Instance?.RunSession?.CityId ?? "city";
            var node = GameContext.Instance?.RunSession?.WorldNodeIndex ?? 0;
            _roomHint = UiFactory.AddText(canvas.transform, "Hint",
                $"CityRun — {city} (node {node + 1}) · multi-room · End buttons",
                15, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.75f),
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
                HitStop.EnsureRunningTimeScale();
                GameContext.Instance?.Runs.AdvanceToNextCity();
            });
        }

        private void End(bool success, int vestiges, string summary)
        {
            if (_ending) return;
            _ending = true;
            HitStop.EnsureRunningTimeScale();
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
