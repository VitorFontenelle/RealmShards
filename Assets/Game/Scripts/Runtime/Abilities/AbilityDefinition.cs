using RealmShards.Magic;
using UnityEngine;

namespace RealmShards
{
    public enum AbilityKind
    {
        Projectile = 0,
        MeleeHitbox = 1,
        Dash = 2
    }

    [CreateAssetMenu(menuName = "RealmShards/Abilities/Ability Definition", fileName = "AbilityDefinition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string contentId = "ability.unnamed";
        [SerializeField] private string displayName = "Ability";
        [SerializeField] private AbilityKind kind = AbilityKind.Projectile;
        [SerializeField] private Sprite icon;
        [SerializeField] private int unlockCost;
        [SerializeField] private string schoolId = "school.neutral";
        [SerializeField] private MagicElement element = MagicElement.Arcane;

        [Header("Timing (seconds)")]
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField] private float windup = 0.05f;
        [SerializeField] private float activeDuration = 0.1f;
        [SerializeField] private float recovery = 0.15f;
        [SerializeField] private float castLockMovement = 0.1f;

        [Header("Combat")]
        [SerializeField] private float damage = 12f;
        [SerializeField] private float knockback = 3.5f;
        [SerializeField] private float range = 8f;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float hitboxRadius = 0.85f;
        [SerializeField] private float hitboxDistance = 0.75f;
        [SerializeField] private bool pierce;
        [SerializeField] private float dashDistance = 3.25f;
        [SerializeField] private float dashDuration = 0.12f;
        [SerializeField] private bool dashInvulnerable = true;
        [SerializeField] private StatusApplication[] statusEffects;

        [Header("Prefabs")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject hitboxPrefab;
        [SerializeField] private GameObject effectOverlayPrefab;

        public string ContentId => contentId;
        public string DisplayName => displayName;
        public AbilityKind Kind => kind;
        public Sprite Icon => icon;
        public int UnlockCost => unlockCost;
        public string SchoolId => schoolId;
        public MagicElement Element => element;
        public StatusApplication[] StatusEffects => statusEffects;
        public float Cooldown => cooldown;
        public float Windup => windup;
        public float ActiveDuration => activeDuration;
        public float Recovery => recovery;
        public float CastLockMovement => castLockMovement;
        public float Damage => damage;
        public float Knockback => knockback;
        public float Range => range;
        public float ProjectileSpeed => projectileSpeed;
        public float HitboxRadius => hitboxRadius;
        public float HitboxDistance => hitboxDistance;
        public bool Pierce => pierce;
        public float DashDistance => dashDistance;
        public float DashDuration => dashDuration;
        public bool DashInvulnerable => dashInvulnerable;
        public GameObject ProjectilePrefab => projectilePrefab;
        public GameObject HitboxPrefab => hitboxPrefab;
        public GameObject EffectOverlayPrefab => effectOverlayPrefab;
        public float TotalCastTime => windup + activeDuration + recovery;

        public void SetPrefabs(GameObject projectile, GameObject hitbox, GameObject overlay)
        {
            projectilePrefab = projectile;
            hitboxPrefab = hitbox;
            effectOverlayPrefab = overlay;
        }

#if UNITY_EDITOR
        public void EditorConfigure(
            string id,
            string name,
            AbilityKind abilityKind,
            float cd,
            float dmg,
            float kb,
            int cost = 0,
            string school = "school.neutral",
            MagicElement elem = MagicElement.Arcane)
        {
            contentId = id;
            displayName = name;
            kind = abilityKind;
            cooldown = cd;
            damage = dmg;
            knockback = kb;
            unlockCost = cost;
            schoolId = school;
            element = elem;
        }

        public void EditorSetStatuses(params StatusApplication[] statuses)
        {
            statusEffects = statuses;
        }
#endif
    }
}
