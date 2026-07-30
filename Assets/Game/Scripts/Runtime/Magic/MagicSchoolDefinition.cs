using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Magic
{
    public enum MagicElement
    {
        Arcane = 0,
        Fire = 1,
        Kinetic = 2,
        Temporal = 3,
        Tide = 4,
        Ash = 5,
        Gold = 6
    }

    public enum StatusEffectType
    {
        None = 0,
        Burn = 1,
        Slow = 2,
        Ward = 3,
        KnockbackWave = 4
    }

    [Serializable]
    public struct StatusApplication
    {
        public StatusEffectType type;
        public float duration;
        public float magnitude;
        public float tickInterval;
    }

    [CreateAssetMenu(menuName = "RealmShards/Magic/Magic School", fileName = "MagicSchool")]
    public sealed class MagicSchoolDefinition : ScriptableObject
    {
        [SerializeField] private string schoolId = "school.neutral";
        [SerializeField] private string displayName = "Neutral Arcana";
        [TextArea] [SerializeField] private string description;
        [SerializeField] private Color accentColor = new Color(0.7f, 0.5f, 0.95f);
        [SerializeField] private Sprite icon;
        [SerializeField] private string[] abilityIds;

        public string SchoolId => schoolId;
        public string DisplayName => displayName;
        public string Description => description;
        public Color AccentColor => accentColor;
        public Sprite Icon => icon;
        public IReadOnlyList<string> AbilityIds => abilityIds;

#if UNITY_EDITOR
        public void EditorConfigure(string id, string name, string desc, Color accent, string[] abilities)
        {
            schoolId = id;
            displayName = name;
            description = desc;
            accentColor = accent;
            abilityIds = abilities;
        }
#endif
    }
}
