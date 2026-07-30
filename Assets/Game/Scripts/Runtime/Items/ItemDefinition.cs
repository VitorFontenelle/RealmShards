using UnityEngine;

namespace RealmShards
{
    public enum ItemKind
    {
        StatBoost = 0,
        EventTrigger = 1,
        AbilityModifier = 2
    }

    [CreateAssetMenu(menuName = "RealmShards/Items/Item Definition", fileName = "ItemDefinition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Item";
        [SerializeField] private string description;
        [SerializeField] private ItemKind kind = ItemKind.StatBoost;
        [SerializeField] private Sprite icon;
        [SerializeField] private Color tint = Color.white;

        [Header("Stat Boost")]
        [SerializeField] private float maxHealthBonus;
        [SerializeField] private float moveSpeedBonus;
        [SerializeField] private float damageMultiplierBonus;

        [Header("Event")]
        [SerializeField] private float healAmount;
        [SerializeField] private bool grantIFrames;
        [SerializeField] private float iFrameDuration = 1f;

        [Header("Ability Modifier")]
        [SerializeField] private float cooldownMultiplier = 0.85f;
        [SerializeField] private float abilityDamageBonus = 4f;
        [SerializeField] private int modifySlot = -1;

        public string DisplayName => displayName;
        public string Description => description;
        public ItemKind Kind => kind;
        public Sprite Icon => icon;
        public Color Tint => tint;
        public float MaxHealthBonus => maxHealthBonus;
        public float MoveSpeedBonus => moveSpeedBonus;
        public float DamageMultiplierBonus => damageMultiplierBonus;
        public float HealAmount => healAmount;
        public bool GrantIFrames => grantIFrames;
        public float IFrameDuration => iFrameDuration;
        public float CooldownMultiplier => cooldownMultiplier;
        public float AbilityDamageBonus => abilityDamageBonus;
        public int ModifySlot => modifySlot;

#if UNITY_EDITOR
        public void EditorConfigure(string name, ItemKind itemKind, string desc)
        {
            displayName = name;
            kind = itemKind;
            description = desc;
        }
#endif
    }
}
