using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Runs
{
    public enum WorldNodeKind
    {
        City = 0,
        Capital = 1
    }

    [Serializable]
    public sealed class WorldRouteNode
    {
        public string cityId;
        public string displayName;
        public WorldNodeKind kind;
        public int index;
        public bool completed;
    }

    [Serializable]
    public sealed class WorldRoutePlan
    {
        public int seed;
        public int preCapitalCount;
        public List<WorldRouteNode> nodes = new List<WorldRouteNode>();

        public int NodeCount => nodes?.Count ?? 0;
        public WorldRouteNode Get(int index)
        {
            if (nodes == null || index < 0 || index >= nodes.Count)
                return null;
            return nodes[index];
        }
    }

    /// <summary>
    /// Deterministic world route: choose among cities without duplicates; capital always last.
    /// </summary>
    public static class WorldRouteGenerator
    {
        public static readonly string[] SampleCityPool =
        {
            Save.ContentIdDefaults.CityStarter,
            Save.ContentIdDefaults.CityGildedWard,
            Save.ContentIdDefaults.CityAshenQuay
        };

        public static WorldRoutePlan Generate(
            int seed,
            int preCapitalCount,
            IReadOnlyList<string> cityPool = null,
            string capitalId = null)
        {
            preCapitalCount = Mathf.Clamp(preCapitalCount, 1, 5);
            capitalId = string.IsNullOrEmpty(capitalId)
                ? Save.ContentIdDefaults.CityCapital
                : capitalId;
            cityPool ??= SampleCityPool;

            var rng = new System.Random(seed);
            var available = new List<string>();
            foreach (var id in cityPool)
            {
                if (!string.IsNullOrEmpty(id) &&
                    !string.Equals(id, capitalId, StringComparison.Ordinal) &&
                    !available.Contains(id))
                {
                    available.Add(id);
                }
            }

            if (available.Count == 0)
                available.Add(Save.ContentIdDefaults.CityStarter);

            var chosen = new List<string>(preCapitalCount);
            var bag = new List<string>(available);
            while (chosen.Count < preCapitalCount)
            {
                if (bag.Count == 0)
                    bag.AddRange(available);

                int pick = rng.Next(bag.Count);
                string id = bag[pick];
                bag.RemoveAt(pick);
                if (!chosen.Contains(id))
                    chosen.Add(id);
                else if (chosen.Count < available.Count)
                    continue;
                else
                    chosen.Add(id); // allow recycle only if pool exhausted beyond unique set
            }

            // Prefer unique when pool size >= requested
            if (available.Count >= preCapitalCount)
            {
                chosen.Clear();
                bag = new List<string>(available);
                Shuffle(bag, rng);
                for (int i = 0; i < preCapitalCount; i++)
                    chosen.Add(bag[i]);
            }

            var plan = new WorldRoutePlan
            {
                seed = seed,
                preCapitalCount = preCapitalCount,
                nodes = new List<WorldRouteNode>()
            };

            for (int i = 0; i < chosen.Count; i++)
            {
                plan.nodes.Add(new WorldRouteNode
                {
                    cityId = chosen[i],
                    displayName = DisplayNameFor(chosen[i]),
                    kind = WorldNodeKind.City,
                    index = i,
                    completed = false
                });
            }

            plan.nodes.Add(new WorldRouteNode
            {
                cityId = capitalId,
                displayName = DisplayNameFor(capitalId),
                kind = WorldNodeKind.Capital,
                index = chosen.Count,
                completed = false
            });

            return plan;
        }

        public static string DisplayNameFor(string cityId)
        {
            return cityId switch
            {
                Save.ContentIdDefaults.CityStarter => "Starter Reach",
                Save.ContentIdDefaults.CityGildedWard => "Gilded Ward",
                Save.ContentIdDefaults.CityAshenQuay => "Ashen Quay",
                Save.ContentIdDefaults.CityCapital => "The Capital",
                _ => cityId
            };
        }

        private static void Shuffle(List<string> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
