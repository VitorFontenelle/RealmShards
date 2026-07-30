using System;
using System.Collections.Generic;
using RealmShards.Core;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Decade-gated champion pick: deterministic from seed + year.
    /// </summary>
    public static class ChampionSelector
    {
        private static readonly List<ChampionDefinition> RuntimePool = new List<ChampionDefinition>();

        public static void RegisterRuntime(ChampionDefinition def)
        {
            if (def == null) return;
            if (!RuntimePool.Contains(def))
                RuntimePool.Add(def);
        }

        public static void ClearRuntimePool() => RuntimePool.Clear();

        public static ChampionDefinition Pick(int seed, int year, IReadOnlyList<ChampionDefinition> pool = null)
        {
            var candidates = new List<ChampionDefinition>();
            if (pool != null)
            {
                for (int i = 0; i < pool.Count; i++)
                    Consider(candidates, pool[i], year);
            }

            for (int i = 0; i < RuntimePool.Count; i++)
                Consider(candidates, RuntimePool[i], year);

#if UNITY_EDITOR
            if (candidates.Count == 0)
                TryLoadFromAssets(candidates, year);
#endif

            if (candidates.Count == 0)
                return CreateFallback(year);

            unchecked
            {
                int mixed = seed ^ (year * 104729) ^ 0x2C1B3C6D;
                var rng = new System.Random(mixed);
                float total = 0f;
                for (int i = 0; i < candidates.Count; i++)
                    total += Mathf.Max(0.01f, candidates[i].Weight);

                float roll = (float)(rng.NextDouble() * total);
                float acc = 0f;
                for (int i = 0; i < candidates.Count; i++)
                {
                    acc += Mathf.Max(0.01f, candidates[i].Weight);
                    if (roll <= acc)
                        return candidates[i];
                }

                return candidates[candidates.Count - 1];
            }
        }

        private static void Consider(List<ChampionDefinition> list, ChampionDefinition def, int year)
        {
            if (def == null || !def.IsAvailableInYear(year))
                return;
            if (!list.Contains(def))
                list.Add(def);
        }

#if UNITY_EDITOR
        private static void TryLoadFromAssets(List<ChampionDefinition> candidates, int year)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ChampionDefinition", new[] { "Assets/Game/Data/Champions" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                var def = UnityEditor.AssetDatabase.LoadAssetAtPath<ChampionDefinition>(path);
                Consider(candidates, def, year);
            }
        }
#endif

        private static ChampionDefinition CreateFallback(int year)
        {
            var enemy = EnemyFactory.CreateDefaultDefinition(EnemyArchetype.Champion);
            // Decade variant: name + tint + HP scale
            int decade = year / 10;
            enemy.ApplyRuntimeDefaults(
                decade >= 2 ? "Ashen Core Warden" : decade >= 1 ? "Gilded Core Sentinel" : "Arcane Core Champion",
                EnemyArchetype.Champion,
                160f + decade * 25f,
                2.0f + decade * 0.05f,
                EnemyFactory.KnightSheet,
                decade >= 2
                    ? new Color(0.85f, 0.35f, 0.25f)
                    : decade >= 1
                        ? new Color(1f, 0.82f, 0.35f)
                        : new Color(0.75f, 0.45f, 1f));
            enemy.ConfigureCombat(0, 6, 20, 6, 1.5f, 0f, 14f + decade * 2f, 1.0f, 0.55f, 1.1f);

            var champ = ScriptableObject.CreateInstance<ChampionDefinition>();
            champ.ConfigureRuntime(
                decade >= 2 ? "champion.ashen_warden" : decade >= 1 ? "champion.gilded_sentinel" : "champion.arcane_core",
                enemy.DisplayName,
                enemy,
                opensArcaneCoreFlag: true,
                minY: 0,
                maxY: 9999,
                w: 1f);
            return champ;
        }
    }
}
