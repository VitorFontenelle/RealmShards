using System;

namespace RealmShards.Runs
{
    public enum RunResultKind
    {
        None = 0,
        Success = 1,
        Failure = 2,
        Aborted = 3
    }

    [Serializable]
    public sealed class RunOutcome
    {
        public RunResultKind kind;
        public string cityId;
        public string routeId;
        public int vestigesEarned;
        public string summary;

        public static RunOutcome Success(string cityId, string routeId, int vestiges, string summary = null)
        {
            return new RunOutcome
            {
                kind = RunResultKind.Success,
                cityId = cityId,
                routeId = routeId,
                vestigesEarned = vestiges,
                summary = summary ?? "City secured."
            };
        }

        public static RunOutcome Failure(string cityId, string routeId, string summary = null)
        {
            return new RunOutcome
            {
                kind = RunResultKind.Failure,
                cityId = cityId,
                routeId = routeId,
                vestigesEarned = 0,
                summary = summary ?? "The decade claims another city."
            };
        }
    }

    /// <summary>
    /// Session state for the active city run. Owned by meta; filled by world/combat.
    /// </summary>
    public sealed class RunSession
    {
        public bool IsActive { get; private set; }
        public string CityId { get; private set; }
        public string RouteId { get; private set; }
        public int Seed { get; private set; }
        public int LocalPlayerCount { get; private set; }
        public RunOutcome LastOutcome { get; private set; }

        public void Begin(string cityId, string routeId, int seed, int localPlayerCount)
        {
            IsActive = true;
            CityId = cityId;
            RouteId = routeId;
            Seed = seed;
            LocalPlayerCount = Math.Clamp(localPlayerCount, 1, 4);
            LastOutcome = null;
        }

        public void Complete(RunOutcome outcome)
        {
            LastOutcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            IsActive = false;
        }

        public void Clear()
        {
            IsActive = false;
            CityId = null;
            RouteId = null;
            Seed = 0;
            LastOutcome = null;
        }
    }

    /// <summary>
    /// World/combat agents implement or call into this to end a run.
    /// </summary>
    public interface IRunHost
    {
        RunSession Session { get; }
        void BeginRun(string cityId, string routeId, int localPlayerCount);
        void EndRun(RunOutcome outcome);
    }
}
