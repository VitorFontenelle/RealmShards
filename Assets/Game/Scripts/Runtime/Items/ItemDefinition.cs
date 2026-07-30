using UnityEngine;

namespace RealmShards
{
    public enum ItemKind
    {
        StatBoost = 0,
        EventTrigger = 1,
        AbilityModifier = 2
    }

    /// <summary>
    /// Data-driven run item. Buffs stack via <see cref="PlayerItemModifiers"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "RealmShards/Items/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string contentId = "item.unnamed";
        [SerializeField] private string displayName = "Item";
        [TextArea(2, 4)]
        [SerializeField] private string flavorText = "A curious scrap of Neutral Arcana.";
        [TextArea(1, 3)]
        [SerializeField] private string description;
        [SerializeField] private ItemKind kind = ItemKind.StatBoost;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color tint = Color.white;

        [Header("Stats")]
        [SerializeField] private float maxHealthBonus;
        [SerializeField] private float moveSpeedBonus;
        [SerializeField] private float damageMultiplierBonus;
        [SerializeField] private float cooldownMultiplier = 1f;

        [Header("Ability Mods")]
        [SerializeField] private bool grantBoltPierce;
        [SerializeField] private int boltSplitExtraProjectiles;
        [SerializeField] private float pulseRadiusBonus;
        [SerializeField] private float blinkDistanceBonus;
        [SerializeField] private float pickupMagnetRadius;
        [SerializeField] private float abilityDamageFlatBonus;

        [Header("On-Hit / Events")]
        [SerializeField] private float onHitHeal;
        [SerializeField] private float onHitVestigeChance;
        [SerializeField] private int onHitVestigeAmount = 1;
        [SerializeField] private float healAmount;
        [SerializeField] private bool grantIFrames;
        [SerializeField] private float iFrameDuration = 1f;

        public string ContentId => contentId;
        public string DisplayName => displayName;
        public string FlavorText => flavorText;
        public string Description => description;
        public ItemKind Kind => kind;
        public Sprite Icon => icon;
        public Color Tint => tint;
        public float MaxHealthBonus => maxHealthBonus;
        public float MoveSpeedBonus => moveSpeedBonus;
        public float DamageMultiplierBonus => damageMultiplierBonus;
        public float CooldownMultiplier => cooldownMultiplier;
        public bool GrantBoltPierce => grantBoltPierce;
        public int BoltSplitExtraProjectiles => boltSplitExtraProjectiles;
        public float PulseRadiusBonus => pulseRadiusBonus;
        public float BlinkDistanceBonus => blinkDistanceBonus;
        public float PickupMagnetRadius => pickupMagnetRadius;
        public float AbilityDamageFlatBonus => abilityDamageFlatBonus;
        public float OnHitHeal => onHitHeal;
        public float OnHitVestigeChance => onHitVestigeChance;
        public int OnHitVestigeAmount => onHitVestigeAmount;
        public float HealAmount => healAmount;
        public bool GrantIFrames => grantIFrames;
        public float IFrameDuration => iFrameDuration;

        public string TooltipBody
        {
            get
            {
                if (!string.IsNullOrEmpty(description))
                    return description;
                return flavorText;
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(string id, string name, ItemKind itemKind, string desc, string flavor)
        {
            contentId = id;
            displayName = name;
            kind = itemKind;
            description = desc;
            flavorText = flavor;
        }

        public void EditorSetIcon(Sprite sprite) => icon = sprite;
#endif
    }
}
