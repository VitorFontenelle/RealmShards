using System;
using System.Collections.Generic;
using RealmShards.Enemies;
using UnityEngine;

namespace RealmShards.Rooms
{
    [CreateAssetMenu(menuName = "RealmShards/Rooms/Encounter Definition", fileName = "EncounterDefinition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [Serializable]
        public struct EnemySpawnEntry
        {
            public EnemyDefinition definition;
            public EnemyArchetype archetypeFallback;
            public int count;
        }

        [SerializeField] private string encounterId = "cityrun-sample";
        [SerializeField] private EnemySpawnEntry[] spawns;
        [SerializeField] private EnemyDefinition championDefinition;
        [SerializeField] private bool spawnChampion = true;
        [SerializeField] private string rewardStubId = "cityrun-clear";

        public string EncounterId => encounterId;
        public IReadOnlyList<EnemySpawnEntry> Spawns => spawns;
        public EnemyDefinition ChampionDefinition => championDefinition;
        public bool SpawnChampion => spawnChampion;
        public string RewardStubId => rewardStubId;

        public void SetRuntime(
            string id,
            EnemySpawnEntry[] entries,
            EnemyDefinition champion,
            bool withChampion,
            string reward)
        {
            encounterId = id;
            spawns = entries;
            championDefinition = champion;
            spawnChampion = withChampion;
            rewardStubId = reward;
        }
    }
}
