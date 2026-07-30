using UnityEngine;

namespace RealmShards.Enemies
{
    [CreateAssetMenu(menuName = "RealmShards/Champions/Champion Definition", fileName = "ChampionDefinition")]
    public sealed class ChampionDefinition : ScriptableObject
    {
        [SerializeField] private string championId = "arcane-core-champion";
        [SerializeField] private string displayName = "Arcane Core Champion";
        [SerializeField] private EnemyDefinition enemyDefinition;
        [SerializeField] private bool opensArcaneCore = true;

        public string ChampionId => championId;
        public string DisplayName => displayName;
        public EnemyDefinition EnemyDefinition => enemyDefinition;
        public bool OpensArcaneCore => opensArcaneCore;
    }
}
