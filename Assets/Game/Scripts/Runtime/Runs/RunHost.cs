using System;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealmShards.Runs
{
    /// <summary>
    /// Default run host: begins CityRun, applies meta rewards/penalties on end.
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
            cityId = string.IsNullOrEmpty(cityId) ? ContentIdDefaults.CityStarter : cityId;
            routeId = string.IsNullOrEmpty(routeId) ? ContentIdDefaults.RouteStarterMain : routeId;
            localPlayerCount = Mathf.Clamp(localPlayerCount, 1, 4);

            var seed = UnityEngine.Random.Range(1, int.MaxValue);
            Session.Begin(cityId, routeId, seed, localPlayerCount);

            var runState = new RunStateData
            {
                cityId = cityId,
                routeId = routeId,
                seed = seed,
                roomIndex = 0,
                isActive = true
            };
            _save.Current.activeRun = runState;
            _save.Current.settings.localPlayerCount = localPlayerCount;
            _save.Save();

            SceneManager.LoadScene(SceneNames.CityRun);
        }

        public void EndRun(RunOutcome outcome)
        {
            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            Session.Complete(outcome);
            _save.Current.activeRun = null;

            switch (outcome.kind)
            {
                case RunResultKind.Success:
                    if (outcome.vestigesEarned > 0)
                    {
                        _progression.AddArcaneVestiges(outcome.vestigesEarned, saveImmediately: false);
                    }

                    _save.Save();
                    break;

                case RunResultKind.Failure:
                    // Spec: failure advances year by +10 and saves.
                    _progression.AdvanceDecadeOnFailure(saveImmediately: true);
                    break;

                default:
                    _save.Save();
                    break;
            }

            SceneManager.LoadScene(SceneNames.RunResults);
        }
    }
}
