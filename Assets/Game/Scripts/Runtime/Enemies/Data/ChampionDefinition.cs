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
        [SerializeField] private int minYear;
        [SerializeField] private int maxYear = 9999;
        [SerializeField] private float weight = 1f;

        public string ChampionId => championId;
        public string DisplayName => displayName;
        public EnemyDefinition EnemyDefinition => enemyDefinition;
        public bool OpensArcaneCore => opensArcaneCore;
        public int MinYear => minYear;
        public int MaxYear => maxYear;
        public float Weight => weight;

        public bool IsAvailableInYear(int year) => year >= minYear && year <= maxYear;

        public void ConfigureRuntime(
            string id,
            string name,
            EnemyDefinition enemy,
            bool opensArcaneCoreFlag,
            int minY,
            int maxY,
            float w)
        {
            championId = id;
            displayName = name;
            enemyDefinition = enemy;
            opensArcaneCore = opensArcaneCoreFlag;
            minYear = minY;
            maxYear = maxY;
            weight = w;
        }

#if UNITY_EDITOR
        public void EditorSetYearRange(int min, int max, float w = 1f)
        {
            minYear = min;
            maxYear = max;
            weight = w;
        }

        public void EditorSetEnemy(EnemyDefinition enemy) => enemyDefinition = enemy;
#endif
    }
}
