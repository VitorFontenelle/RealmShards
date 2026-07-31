using System;
using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.Rooms
{
    /// <summary>
    /// Locks on start, spawns enemies from data, unlocks on clear, fires reward stub.
    /// </summary>
    public sealed class EncounterRoom : MonoBehaviour
    {
        [SerializeField] private EncounterDefinition encounter;
        [SerializeField] private Enemies.CoopScalingConfig coopScaling;
        [SerializeField] private RoomBounds roomBounds;
        [SerializeField] private bool autoStart = false;
        [SerializeField] private bool lockExitsOnStart = true;
        [SerializeField] private Transform[] exitBlockers;
        [SerializeField] private List<SpawnPoint> enemySpawns = new List<SpawnPoint>();
        [SerializeField] private List<SpawnPoint> championSpawns = new List<SpawnPoint>();

        private readonly List<Health> _alive = new List<Health>();
        private bool _started;
        private bool _cleared;
        private bool _locked;

        public bool IsCleared => _cleared;
        public bool IsLocked => _locked;
        public event Action<EncounterRoom> Cleared;
        public event Action<string> RewardGranted;

        public void Configure(
            EncounterDefinition def,
            Enemies.CoopScalingConfig scaling,
            RoomBounds bounds,
            IEnumerable<SpawnPoint> enemies,
            IEnumerable<SpawnPoint> champions)
        {
            encounter = def;
            coopScaling = scaling;
            roomBounds = bounds;
            enemySpawns.Clear();
            championSpawns.Clear();
            if (enemies != null) enemySpawns.AddRange(enemies);
            if (champions != null) championSpawns.AddRange(champions);
        }

        private void Start()
        {
            if (autoStart)
                BeginEncounter();
        }

        public void BeginEncounter()
        {
            if (_started)
                return;
            _started = true;

            if (lockExitsOnStart)
                SetLocked(true);

            SpawnWave();
        }

        private void SpawnWave()
        {
            int players = Enemies.PlayerTargetRegistry.CountAlive();
            float hpMul = coopScaling != null ? coopScaling.GetHealthMultiplier(players) : 1f;
            float dmgMul = coopScaling != null ? coopScaling.GetDamageMultiplier(players) : 1f;

            int spawnIndex = 0;

            if (encounter != null && encounter.Spawns != null)
            {
                foreach (var entry in encounter.Spawns)
                {
                    int count = entry.count;
                    if (coopScaling != null)
                        count = coopScaling.ScaleSpawnCount(count, players);

                    for (int i = 0; i < count; i++)
                    {
                        Vector3 pos = NextEnemyPosition(ref spawnIndex);
                        var def = entry.definition;
                        var archetype = def != null ? def.Archetype : entry.archetypeFallback;
                        var enemy = Enemies.EnemyFactory.Spawn(archetype, def, pos, hpMul, dmgMul);
                        Track(enemy);
                    }
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    var e = Enemies.EnemyFactory.Spawn(Enemies.EnemyArchetype.Warrior, null, NextEnemyPosition(ref spawnIndex), hpMul, dmgMul);
                    Track(e);
                }
                for (int i = 0; i < 2; i++)
                {
                    var e = Enemies.EnemyFactory.Spawn(Enemies.EnemyArchetype.Archer, null, NextEnemyPosition(ref spawnIndex), hpMul, dmgMul);
                    Track(e);
                }
            }

            if (encounter == null || encounter.SpawnChampion)
            {
                Vector3 cPos = championSpawns.Count > 0
                    ? championSpawns[0].Position
                    : (roomBounds != null ? roomBounds.Center + new Vector3(0f, 4f, 0f) : transform.position + Vector3.up * 4f);

                var champDef = encounter != null ? encounter.ChampionDefinition : null;
                var champ = Enemies.EnemyFactory.Spawn(Enemies.EnemyArchetype.Champion, champDef, cPos, hpMul * 1.2f, dmgMul);
                Track(champ);
            }

            if (_alive.Count == 0)
                Complete();
        }

        private Vector3 NextEnemyPosition(ref int index)
        {
            if (enemySpawns.Count > 0)
            {
                var sp = enemySpawns[index % enemySpawns.Count];
                index++;
                return sp.Position;
            }

            float angle = index * 55f * Mathf.Deg2Rad;
            index++;
            Vector3 center = roomBounds != null ? roomBounds.Center : transform.position;
            return center + new Vector3(Mathf.Cos(angle) * 5f, Mathf.Sin(angle) * 3.5f, 0f);
        }

        private void Track(Enemies.EnemyBrainBase brain)
        {
            if (brain == null)
                return;
            var hp = brain.GetComponent<Health>();
            if (hp == null)
                return;
            _alive.Add(hp);
            hp.Died += OnEnemyDied;
        }

        private void OnEnemyDied(Health health)
        {
            health.Died -= OnEnemyDied;
            _alive.Remove(health);
            RunCurrencyRewards.OnOpponentDefeated();
            if (_alive.Count == 0)
                Complete();
        }

        private void Complete()
        {
            if (_cleared)
                return;
            _cleared = true;
            SetLocked(false);
            string reward = encounter != null ? encounter.RewardStubId : "cityrun-clear";
            Debug.Log($"[RealmShards] Encounter cleared. Reward stub: {reward}");
            RewardGranted?.Invoke(reward);
            Cleared?.Invoke(this);
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (exitBlockers == null)
                return;
            for (int i = 0; i < exitBlockers.Length; i++)
            {
                if (exitBlockers[i] != null)
                    exitBlockers[i].gameObject.SetActive(locked);
            }
        }

        public void SetExitBlockers(Transform[] blockers)
        {
            exitBlockers = blockers;
        }
    }
}
