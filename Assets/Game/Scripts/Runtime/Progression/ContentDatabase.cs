using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Progression
{
    public enum ContentKind
    {
        Ability,
        City,
        Route,
        Enemy,
        Item,
        School
    }

    [Serializable]
    public sealed class ContentEntry
    {
        public string id;
        public ContentKind kind;
        public string displayName;
        [TextArea] public string description;
    }

    [CreateAssetMenu(fileName = "ContentDatabase", menuName = "RealmShards/Progression/Content Database")]
    public sealed class ContentDatabase : ScriptableObject
    {
        [SerializeField] private List<ContentEntry> entries = new List<ContentEntry>();

        private Dictionary<string, ContentEntry> _lookup;

        public IReadOnlyList<ContentEntry> Entries => entries;

        public void RebuildLookup()
        {
            _lookup = new Dictionary<string, ContentEntry>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;
                _lookup[entry.id] = entry;
            }
        }

        public bool TryGet(string id, out ContentEntry entry)
        {
            if (_lookup == null) RebuildLookup();
            return _lookup.TryGetValue(id, out entry);
        }

        public ContentEntry GetOrNull(string id) => TryGet(id, out var entry) ? entry : null;

        public string GetDisplayName(string id, string fallback = null)
        {
            var entry = GetOrNull(id);
            if (entry != null && !string.IsNullOrEmpty(entry.displayName))
                return entry.displayName;
            return fallback ?? id ?? string.Empty;
        }

        public static ContentDatabase CreateRuntimeDefault()
        {
            var db = CreateInstance<ContentDatabase>();
            db.entries = new List<ContentEntry>
            {
                E(Save.ContentIdDefaults.AbilityBasicBolt, ContentKind.Ability, "Arcane Bolt"),
                E(Save.ContentIdDefaults.AbilityArcanePulse, ContentKind.Ability, "Arcane Pulse"),
                E(Save.ContentIdDefaults.AbilityBlinkStep, ContentKind.Ability, "Blink Step"),
                E(Save.ContentIdDefaults.AbilityGildedFlare, ContentKind.Ability, "Gilded Flare"),
                E(Save.ContentIdDefaults.AbilityGildedSmite, ContentKind.Ability, "Gilded Smite"),
                E(Save.ContentIdDefaults.AbilityAshenDrift, ContentKind.Ability, "Ashen Drift"),
                E(Save.ContentIdDefaults.AbilityAshenCinder, ContentKind.Ability, "Ashen Cinder"),
                E(Save.ContentIdDefaults.AbilityTideglassRipple, ContentKind.Ability, "Tideglass Ripple"),
                E(Save.ContentIdDefaults.AbilityTideglassHarpoon, ContentKind.Ability, "Tideglass Harpoon"),
                E(Save.ContentIdDefaults.AbilityContinuumSlip, ContentKind.Ability, "Continuum Slip"),
                E(Save.ContentIdDefaults.AbilityContinuumEcho, ContentKind.Ability, "Continuum Echo"),
                E(Save.ContentIdDefaults.CityStarter, ContentKind.City, "Starter Reach"),
                E(Save.ContentIdDefaults.CityGildedWard, ContentKind.City, "Gilded Ward"),
                E(Save.ContentIdDefaults.CityAshenQuay, ContentKind.City, "Ashen Quay"),
                E(Save.ContentIdDefaults.CityCapital, ContentKind.City, "The Capital"),
                E(Save.ContentIdDefaults.RouteWorldMain, ContentKind.Route, "World Route"),
                E(Save.ContentIdDefaults.SchoolNeutral, ContentKind.School, "Neutral Arcana"),
                E(Save.ContentIdDefaults.SchoolGilded, ContentKind.School, "Gilded Ward"),
                E(Save.ContentIdDefaults.SchoolAshen, ContentKind.School, "Ashen Veil"),
                E(Save.ContentIdDefaults.SchoolTideglass, ContentKind.School, "Tideglass"),
                E(Save.ContentIdDefaults.SchoolContinuum, ContentKind.School, "Continuum"),
            };
            db.RebuildLookup();
            return db;
        }

        private static ContentEntry E(string id, ContentKind kind, string name) =>
            new ContentEntry { id = id, kind = kind, displayName = name, description = name };
    }
}
