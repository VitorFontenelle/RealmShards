using System.Collections.Generic;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Runtime lookup of ability definitions by stable content ID.
    /// Populated from Resources or editor setup; falls back to Resources.LoadAll.
    /// </summary>
    public static class AbilityCatalog
    {
        private static Dictionary<string, AbilityDefinition> _byId;
        private static bool _loaded;

        public static void Register(AbilityDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.ContentId))
                return;
            Ensure();
            _byId[definition.ContentId] = definition;
        }

        public static void RegisterAll(IEnumerable<AbilityDefinition> defs)
        {
            if (defs == null) return;
            foreach (var d in defs)
                Register(d);
        }

        public static AbilityDefinition Get(string contentId)
        {
            if (string.IsNullOrEmpty(contentId))
                return null;
            Ensure();
            return _byId.TryGetValue(contentId, out var def) ? def : null;
        }

        public static IEnumerable<AbilityDefinition> All
        {
            get
            {
                Ensure();
                return _byId.Values;
            }
        }

        public static void Clear()
        {
            _byId?.Clear();
            _loaded = false;
        }

        private static void Ensure()
        {
            if (_loaded && _byId != null)
                return;

            _byId = new Dictionary<string, AbilityDefinition>(System.StringComparer.Ordinal);
            _loaded = true;

            var loaded = Resources.LoadAll<AbilityDefinition>("Abilities");
            if (loaded != null)
            {
                foreach (var d in loaded)
                    if (d != null && !string.IsNullOrEmpty(d.ContentId))
                        _byId[d.ContentId] = d;
            }

#if UNITY_EDITOR
            if (_byId.Count == 0)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:AbilityDefinition");
                foreach (var guid in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
                    if (def != null && !string.IsNullOrEmpty(def.ContentId))
                        _byId[def.ContentId] = def;
                }
            }
#endif
        }
    }

    public static class ItemCatalog
    {
        private static Dictionary<string, ItemDefinition> _byId;
        private static bool _loaded;

        public static void Register(ItemDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.ContentId))
                return;
            Ensure();
            _byId[definition.ContentId] = definition;
        }

        public static ItemDefinition Get(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return null;
            Ensure();
            return _byId.TryGetValue(contentId, out var def) ? def : null;
        }

        public static IEnumerable<ItemDefinition> All
        {
            get
            {
                Ensure();
                return _byId.Values;
            }
        }

        private static void Ensure()
        {
            if (_loaded && _byId != null) return;
            _byId = new Dictionary<string, ItemDefinition>(System.StringComparer.Ordinal);
            _loaded = true;
            var loaded = Resources.LoadAll<ItemDefinition>("Items");
            if (loaded != null)
            {
                foreach (var d in loaded)
                    if (d != null && !string.IsNullOrEmpty(d.ContentId))
                        _byId[d.ContentId] = d;
            }
#if UNITY_EDITOR
            if (_byId.Count == 0)
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition");
                foreach (var guid in guids)
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    var def = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                    if (def != null && !string.IsNullOrEmpty(def.ContentId))
                        _byId[def.ContentId] = def;
                }
            }
#endif
        }
    }
}
