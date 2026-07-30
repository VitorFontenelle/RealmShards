using UnityEngine;

namespace RealmShards.Enemies
{
    public enum EnemyArchetype
    {
        Warrior,
        Archer,
        Champion
    }

    [CreateAssetMenu(menuName = "RealmShards/Enemies/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private EnemyArchetype archetype = EnemyArchetype.Warrior;
        [SerializeField] private float maxHealth = 40f;
        [SerializeField] private float moveSpeed = 2.4f;
        [SerializeField] private float aggroRange = 5.5f;
        [SerializeField] private float attackRange = 1.35f;
        [SerializeField] private float attackDamage = 8f;
        [SerializeField] private float telegraphDuration = 0.45f;
        [SerializeField] private float activeHitDuration = 0.18f;
        [SerializeField] private float attackCooldown = 1.1f;
        [SerializeField] private float hitboxRadius = 0.85f;
        [SerializeField] private float hitboxForwardOffset = 0.7f;
        [SerializeField] private float preferredDistance = 5.5f;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private float projectileLifetime = 3f;
        [SerializeField] private float retargetInterval = 1.25f;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private string spritesheetAssetPath;
        [SerializeField] private int walkFrameStart;
        [SerializeField] private int walkFrameCount = 4;
        [SerializeField] private int attackFrameStart;
        [SerializeField] private int attackFrameCount = 4;
        [SerializeField] private float animFps = 8f;
        [SerializeField] private GameObject prefabOverride;

        public string DisplayName => displayName;
        public EnemyArchetype Archetype => archetype;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float AggroRange => aggroRange;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float TelegraphDuration => telegraphDuration;
        public float ActiveHitDuration => activeHitDuration;
        public float AttackCooldown => attackCooldown;
        public float HitboxRadius => hitboxRadius;
        public float HitboxForwardOffset => hitboxForwardOffset;
        public float PreferredDistance => preferredDistance;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public float RetargetInterval => retargetInterval;
        public Color Tint => tint;
        public string SpritesheetAssetPath => spritesheetAssetPath;
        public int WalkFrameStart => walkFrameStart;
        public int WalkFrameCount => walkFrameCount;
        public int AttackFrameStart => attackFrameStart;
        public int AttackFrameCount => attackFrameCount;
        public float AnimFps => animFps;
        public GameObject PrefabOverride => prefabOverride;

        public void ApplyRuntimeDefaults(
            string name,
            EnemyArchetype type,
            float hp,
            float speed,
            string sheetPath,
            Color color)
        {
            displayName = name;
            archetype = type;
            maxHealth = hp;
            moveSpeed = speed;
            spritesheetAssetPath = sheetPath;
            tint = color;
        }

        public void ConfigureCombat(
            int walkStart,
            int walkCount,
            int atkStart,
            int atkCount,
            float range,
            float preferred,
            float damage,
            float cooldown,
            float telegraph,
            float hitRadius = 0.85f)
        {
            walkFrameStart = walkStart;
            walkFrameCount = walkCount;
            attackFrameStart = atkStart;
            attackFrameCount = atkCount;
            attackRange = range;
            preferredDistance = preferred;
            attackDamage = damage;
            attackCooldown = cooldown;
            telegraphDuration = telegraph;
            hitboxRadius = hitRadius;
        }

        public void ConfigureAggro(float aggro, float retarget = 0.9f)
        {
            aggroRange = Mathf.Max(1.5f, aggro);
            retargetInterval = Mathf.Max(0.2f, retarget);
        }
    }
}
