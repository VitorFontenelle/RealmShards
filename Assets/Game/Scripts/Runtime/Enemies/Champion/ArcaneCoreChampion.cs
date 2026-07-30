using System;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// High-HP warrior variant / sample elite. On death opens an Arcane Core stub trigger.
    /// </summary>
    public sealed class ArcaneCoreChampion : GoldenAxeWarrior
    {
        [SerializeField] private bool spawnArcaneCoreOnDeath = true;
        [SerializeField] private GameObject arcaneCorePrefab;

        public event Action<ArcaneCoreChampion> ChampionDefeated;

        protected override void OnEnemyDied()
        {
            base.OnEnemyDied();
            ChampionDefeated?.Invoke(this);

            if (!spawnArcaneCoreOnDeath)
                return;

            if (arcaneCorePrefab != null)
            {
                Instantiate(arcaneCorePrefab, transform.position, Quaternion.identity);
            }
            else
            {
                ArcaneCoreTrigger.SpawnStub(transform.position);
            }
        }
    }
}
