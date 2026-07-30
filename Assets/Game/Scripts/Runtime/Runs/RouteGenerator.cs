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

    /// <summary>
    /// Minimal linear route generator for Stage 2 (foundation may replace).
    /// </summary>
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
        [SerializeField] private string cityId = "sample-city";
        [SerializeField] private string displayName = "Sample City";
        [SerializeField] private string sceneName = "CityRun";
        [SerializeField] private int encounterCount = 3;
        [SerializeField] private bool includeElite = true;

        public string CityId => cityId;
        public string DisplayName => displayName;
        public string SceneName => sceneName;

        public List<RouteNode> BuildRoute() => RouteGenerator.GenerateSampleCityRoute(encounterCount, includeElite);
    }
}
