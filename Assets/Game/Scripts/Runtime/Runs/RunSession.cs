using System;
using System.Collections.Generic;
using UnityEngine;

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
    /// Session state for the active world run. Owned by meta; filled by world/combat.
    /// </summary>
    public sealed class RunSession
    {
        public bool IsActive { get; private set; }
        public string CityId { get; private set; }
        public string RouteId { get; private set; }
        public int Seed { get; private set; }
        public int LocalPlayerCount { get; private set; }
        public int WorldNodeIndex { get; private set; }
        public int RoomIndex { get; set; }
        public bool IsCapitalNode { get; private set; }
        public bool AwaitingArcaneCore { get; set; }
        public WorldRoutePlan RoutePlan { get; private set; }
        public IReadOnlyList<string> LoadoutAbilityIds { get; private set; }
        public IReadOnlyList<IReadOnlyList<string>> LoadoutsByPlayer { get; private set; }
        public IReadOnlyList<string> SelectedItemIdsByPlayer { get; private set; }
        public RunOutcome LastOutcome { get; private set; }

        public void Begin(
            string cityId,
            string routeId,
            int seed,
            int localPlayerCount,
            WorldRoutePlan plan = null,
            int worldNodeIndex = 0,
            IList<string> loadout = null,
            IList<IReadOnlyList<string>> loadoutsByPlayer = null,
            IList<string> selectedItemsByPlayer = null)
        {
            IsActive = true;
            CityId = cityId;
            RouteId = routeId;
            Seed = seed;
            LocalPlayerCount = Math.Clamp(localPlayerCount, 1, 4);
            WorldNodeIndex = Mathf.Max(0, worldNodeIndex);
            RoomIndex = 0;
            RoutePlan = plan;
            IsCapitalNode = plan?.Get(WorldNodeIndex)?.kind == WorldNodeKind.Capital
                            || string.Equals(cityId, Save.ContentIdDefaults.CityCapital, StringComparison.Ordinal);
            AwaitingArcaneCore = false;
            LoadoutAbilityIds = loadout != null
                ? new List<string>(loadout)
                : new List<string>();
            LoadoutsByPlayer = loadoutsByPlayer != null
                ? new List<IReadOnlyList<string>>(loadoutsByPlayer)
                : new List<IReadOnlyList<string>>();
            SelectedItemIdsByPlayer = selectedItemsByPlayer != null
                ? new List<string>(selectedItemsByPlayer)
                : new List<string>();
            LastOutcome = null;
        }

        public void AdvanceToNode(int nodeIndex)
        {
            if (RoutePlan == null)
                return;
            WorldNodeIndex = Mathf.Clamp(nodeIndex, 0, RoutePlan.NodeCount - 1);
            var node = RoutePlan.Get(WorldNodeIndex);
            if (node != null)
            {
                CityId = node.cityId;
                IsCapitalNode = node.kind == WorldNodeKind.Capital;
            }

            RoomIndex = 0;
            AwaitingArcaneCore = false;
        }

        public void MarkCurrentNodeCompleted()
        {
            var node = RoutePlan?.Get(WorldNodeIndex);
            if (node != null)
                node.completed = true;
        }

        public bool HasNextNode => RoutePlan != null && WorldNodeIndex + 1 < RoutePlan.NodeCount;

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
            WorldNodeIndex = 0;
            RoomIndex = 0;
            IsCapitalNode = false;
            AwaitingArcaneCore = false;
            RoutePlan = null;
            LoadoutAbilityIds = null;
            LoadoutsByPlayer = null;
            SelectedItemIdsByPlayer = null;
            LastOutcome = null;
        }
    }

    public interface IRunHost
    {
        RunSession Session { get; }
        void BeginRun(string cityId, string routeId, int localPlayerCount);
        void BeginWorldRun(int preCapitalCount, int localPlayerCount, int? seed = null);
        void AdvanceToNextCity();
        void EndRun(RunOutcome outcome);
    }
}
