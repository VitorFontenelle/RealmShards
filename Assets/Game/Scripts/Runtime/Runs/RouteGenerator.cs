using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Runs
{
    public enum RouteNodeType
    {
        Encounter,
        Elite,
        Rest,
        BossStub
    }

    [System.Serializable]
    public struct RouteNode
    {
        public string id;
        public RouteNodeType type;
        public int depth;
    }

    /// <summary>Within-city encounter route (rooms before champion).</summary>
    public static class RouteGenerator
    {
        public static List<RouteNode> GenerateSampleCityRoute(int encounterCount = 3, bool includeElite = true)
        {
            var nodes = new List<RouteNode>(encounterCount + 2);
            for (int i = 0; i < encounterCount; i++)
            {
                nodes.Add(new RouteNode
                {
                    id = $"encounter-{i + 1}",
                    type = RouteNodeType.Encounter,
                    depth = i
                });
            }

            if (includeElite)
            {
                nodes.Add(new RouteNode
                {
                    id = "elite-arcane",
                    type = RouteNodeType.Elite,
                    depth = encounterCount
                });
            }

            nodes.Add(new RouteNode
            {
                id = "boss-stub",
                type = RouteNodeType.BossStub,
                depth = encounterCount + (includeElite ? 1 : 0)
            });

            return nodes;
        }
    }

    [CreateAssetMenu(menuName = "RealmShards/Cities/City Definition", fileName = "CityDefinition")]
    public sealed class CityDefinition : ScriptableObject
    {
        [SerializeField] private string cityId = "city.starter";
        [SerializeField] private string displayName = "Sample City";
        [SerializeField] private string sceneName = "CityRun";
        [SerializeField] private int encounterCount = 2;
        [SerializeField] private bool includeElite;
        [SerializeField] private bool isCapital;
        [SerializeField] private string magicSchoolId = "school.neutral";
        [SerializeField] private string[] unlockableAbilityIds =
        {
            Save.ContentIdDefaults.AbilityArcanePulse,
            Save.ContentIdDefaults.AbilityBlinkStep
        };
        [SerializeField] private int[] unlockCosts = { 15, 20 };

        public string CityId => cityId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;
        public bool IsCapital => isCapital;
        public string MagicSchoolId => magicSchoolId;
        public IReadOnlyList<string> UnlockableAbilityIds => unlockableAbilityIds;
        public IReadOnlyList<int> UnlockCostsList => unlockCosts;

        public List<RouteNode> BuildRoute() => RouteGenerator.GenerateSampleCityRoute(encounterCount, includeElite);

        public int GetUnlockCost(int index)
        {
            if (unlockCosts == null || index < 0 || index >= unlockCosts.Length)
                return 15;
            return unlockCosts[index];
        }

#if UNITY_EDITOR
        public void EditorConfigure(string id, string name, bool capital, int rooms, string schoolId)
        {
            cityId = id;
            displayName = name;
            isCapital = capital;
            encounterCount = rooms;
            includeElite = !capital && rooms >= 2;
            magicSchoolId = schoolId;
        }
#endif
    }
}
