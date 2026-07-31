using System;
using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmShards.Runs
{
    /// <summary>
    /// Default run host: world route → CityRun nodes → capital last → results.
    /// </summary>
    public sealed class RunHost : IRunHost
    {
        private readonly ISaveService _save;
        private readonly ProgressionService _progression;

        public RunHost(ISaveService save, ProgressionService progression)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            Session = new RunSession();
        }

        public RunSession Session { get; }

        public void BeginRun(string cityId, string routeId, int localPlayerCount)
        {
            // Legacy single-city entry — wrap as 1-city + capital route.
            BeginWorldRun(
                preCapitalCount: 1,
                localPlayerCount: localPlayerCount,
                seed: null,
                forceFirstCity: string.IsNullOrEmpty(cityId) ? ContentIdDefaults.CityStarter : cityId);
            _ = routeId;
        }

        public void BeginWorldRun(int preCapitalCount, int localPlayerCount, int? seed = null)
        {
            BeginWorldRun(preCapitalCount, localPlayerCount, seed, forceFirstCity: null);
        }

        private void BeginWorldRun(int preCapitalCount, int localPlayerCount, int? seed, string forceFirstCity)
        {
            localPlayerCount = Mathf.Clamp(localPlayerCount, 1, 4);
            preCapitalCount = Mathf.Clamp(preCapitalCount, 1, 5);
            int useSeed = seed ?? UnityEngine.Random.Range(1, int.MaxValue);

            var plan = WorldRouteGenerator.Generate(useSeed, preCapitalCount);
            if (!string.IsNullOrEmpty(forceFirstCity) && plan.nodes.Count > 1)
            {
                plan.nodes[0].cityId = forceFirstCity;
                plan.nodes[0].displayName = WorldRouteGenerator.DisplayNameFor(forceFirstCity);
            }

            var first = plan.Get(0);
            var meta = _save.Current.meta;
            var loadouts = PlayerLoadoutService.GetAllEquipped(meta, localPlayerCount);
            var loadout = loadouts.Count > 0
                ? new List<string>(loadouts[0])
                : new List<string>(meta.equippedAbilityIds);
            Session.Begin(
                first.cityId,
                ContentIdDefaults.RouteWorldMain,
                useSeed,
                localPlayerCount,
                plan,
                0,
                loadout,
                loadouts);

            PersistActiveRun();
            _save.Current.settings.localPlayerCount = localPlayerCount;
            _save.Current.meta.preferredPreCapitalNodes = preCapitalCount;
            _save.Save();

            SceneManager.LoadScene(SceneNames.CityRun);
        }

        public void AdvanceToNextCity()
        {
            if (!Session.IsActive || !Session.HasNextNode)
            {
                EndRun(RunOutcome.Success(
                    Session.CityId ?? ContentIdDefaults.CityCapital,
                    Session.RouteId ?? ContentIdDefaults.RouteWorldMain,
                    40,
                    "Route complete — Capital secured."));
                return;
            }

            Session.MarkCurrentNodeCompleted();
            Session.AdvanceToNode(Session.WorldNodeIndex + 1);
            PersistActiveRun();
            _save.Save();
            SceneManager.LoadScene(SceneNames.CityRun);
        }

        public void EndRun(RunOutcome outcome)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));

            Session.Complete(outcome);
            _save.Current.activeRun = null;

            switch (outcome.kind)
            {
                case RunResultKind.Success:
                    if (outcome.vestigesEarned > 0)
                        _progression.AddArcaneVestiges(outcome.vestigesEarned, saveImmediately: false);
                    _save.Save();
                    break;

                case RunResultKind.Failure:
                    _progression.AdvanceDecadeOnFailure(saveImmediately: true);
                    break;

                default:
                    _save.Save();
                    break;
            }

            SceneManager.LoadScene(SceneNames.RunResults);
        }

        private void PersistActiveRun()
        {
            var planJson = Session.RoutePlan != null
                ? JsonUtility.ToJson(Session.RoutePlan)
                : null;

            _save.Current.activeRun = new RunStateData
            {
                cityId = Session.CityId,
                routeId = Session.RouteId,
                seed = Session.Seed,
                roomIndex = Session.RoomIndex,
                worldNodeIndex = Session.WorldNodeIndex,
                isActive = true,
                isCapital = Session.IsCapitalNode,
                routePlanJson = planJson
            };
        }
    }
}
