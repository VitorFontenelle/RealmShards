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
        Item
    }

    [Serializable]
    public sealed class ContentEntry
    {
        public string id;
        public ContentKind kind;
        public string displayName;
        [TextArea] public string description;
    }

    /// <summary>
    /// ScriptableObject catalog for stable string ID resolution.
    /// Other agents register or look up IDs here — do not use asset fileIDs in saves.
    /// </summary>
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
                {
                    continue;
                }

                _lookup[entry.id] = entry;
            }
        }

        public bool TryGet(string id, out ContentEntry entry)
        {
            if (_lookup == null)
            {
                RebuildLookup();
            }

            return _lookup.TryGetValue(id, out entry);
        }

        public ContentEntry GetOrNull(string id)
        {
            return TryGet(id, out var entry) ? entry : null;
        }

        public string GetDisplayName(string id, string fallback = null)
        {
            var entry = GetOrNull(id);
            if (entry != null && !string.IsNullOrEmpty(entry.displayName))
            {
                return entry.displayName;
            }

            return fallback ?? id ?? string.Empty;
        }

        /// <summary>Runtime stub catalog used when no asset is assigned.</summary>
        public static ContentDatabase CreateRuntimeDefault()
        {
            var db = CreateInstance<ContentDatabase>();
            db.entries = new List<ContentEntry>
            {
                new ContentEntry
                {
                    id = Save.ContentIdDefaults.AbilityBasicBolt,
                    kind = ContentKind.Ability,
                    displayName = "Basic Bolt",
                    description = "Placeholder starter projectile ability."
                },
                new ContentEntry
                {
                    id = Save.ContentIdDefaults.AbilityDash,
                    kind = ContentKind.Ability,
                    displayName = "Dash",
                    description = "Placeholder movement ability (locked until unlocked)."
                },
                new ContentEntry
                {
                    id = Save.ContentIdDefaults.CityStarter,
                    kind = ContentKind.City,
                    displayName = "Starter City",
                    description = "First playable city route."
                },
                new ContentEntry
                {
                    id = Save.ContentIdDefaults.RouteStarterMain,
                    kind = ContentKind.Route,
                    displayName = "Main Route",
                    description = "Default route through the starter city."
                }
            };
            db.RebuildLookup();
            return db;
        }
    }
}
