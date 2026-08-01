using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Loads per-spell vial artwork from Resources/SpellVials/{ability_suffix}.png.
    /// </summary>
    public static class SpellVialSprites
    {
        private const string ResourceFolder = "SpellVials";
        private const float SpritePpu = 100f;
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string abilityContentId)
        {
            if (string.IsNullOrEmpty(abilityContentId))
                return null;

            if (Cache.TryGetValue(abilityContentId, out var cached))
                return cached;

            string fileName = ToResourceName(abilityContentId);
            var tex = Resources.Load<Texture2D>($"{ResourceFolder}/{fileName}");
            if (tex == null)
                return null;

            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpritePpu);
            Cache[abilityContentId] = sprite;
            return sprite;
        }

        public static Sprite GetForAbility(AbilityDefinition definition)
        {
            if (definition == null)
                return null;
            return Get(definition.ContentId) ?? definition.Icon;
        }

        private static string ToResourceName(string abilityContentId)
        {
            const string prefix = "ability.";
            return abilityContentId.StartsWith(prefix)
                ? abilityContentId.Substring(prefix.Length)
                : abilityContentId;
        }
    }
}
