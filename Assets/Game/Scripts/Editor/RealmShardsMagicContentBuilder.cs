#if UNITY_EDITOR
using RealmShards.Magic;
using RealmShards.Save;
using UnityEditor;
using UnityEngine;

namespace RealmShards.Editor
{
    public static class RealmShardsMagicContentBuilder
    {
        [MenuItem("RealmShards/Setup Magic Schools")]
        public static void BuildMenu() => BuildAll();

        public static void BuildAll()
        {
            EnsureFolder("Assets/Game/Data/Abilities");
            EnsureFolder("Assets/Game/Data/Magic");
            EnsureFolder("Assets/Game/Data/Cities");

            var projectile = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Projectiles/ArcaneBolt.prefab");
            var hitbox = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Combat/MeleeHitbox.prefab");
            var overlay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Combat/CastOverlay.prefab");

            // Neutral
            var bolt = MakeAbility("ArcaneBolt", ContentIdDefaults.AbilityBasicBolt, "Arcane Bolt",
                AbilityKind.Projectile, 0.35f, 14f, 2.5f, 0, ContentIdDefaults.SchoolNeutral, MagicElement.Arcane);
            var pulse = MakeAbility("ArcanePulse", ContentIdDefaults.AbilityArcanePulse, "Arcane Pulse",
                AbilityKind.MeleeHitbox, 0.55f, 18f, 5f, 15, ContentIdDefaults.SchoolNeutral, MagicElement.Arcane);
            var blink = MakeAbility("BlinkStep", ContentIdDefaults.AbilityBlinkStep, "Blink Step",
                AbilityKind.Dash, 0.85f, 0f, 0f, 20, ContentIdDefaults.SchoolNeutral, MagicElement.Kinetic);
            Wire(bolt, projectile, hitbox, overlay);
            Wire(pulse, projectile, hitbox, overlay);
            Wire(blink, projectile, hitbox, overlay);

            // Gilded Ward — Fire/Gold
            var flare = MakeAbility("GildedFlare", ContentIdDefaults.AbilityGildedFlare, "Gilded Flare",
                AbilityKind.Projectile, 0.4f, 12f, 2f, 18, ContentIdDefaults.SchoolGilded, MagicElement.Fire);
            flare.EditorSetStatuses(new StatusApplication { type = StatusEffectType.Burn, duration = 3f, magnitude = 3f, tickInterval = 0.5f });
            var smite = MakeAbility("GildedSmite", ContentIdDefaults.AbilityGildedSmite, "Gilded Smite",
                AbilityKind.MeleeHitbox, 0.7f, 22f, 6f, 22, ContentIdDefaults.SchoolGilded, MagicElement.Gold);
            smite.EditorSetStatuses(new StatusApplication { type = StatusEffectType.KnockbackWave, duration = 0.1f, magnitude = 8f });
            Wire(flare, projectile, hitbox, overlay);
            Wire(smite, projectile, hitbox, overlay);

            // Ashen Veil
            var drift = MakeAbility("AshenDrift", ContentIdDefaults.AbilityAshenDrift, "Ashen Drift",
                AbilityKind.Dash, 0.9f, 0f, 0f, 16, ContentIdDefaults.SchoolAshen, MagicElement.Ash);
            var cinder = MakeAbility("AshenCinder", ContentIdDefaults.AbilityAshenCinder, "Ashen Cinder",
                AbilityKind.Projectile, 0.45f, 10f, 1.5f, 18, ContentIdDefaults.SchoolAshen, MagicElement.Fire);
            cinder.EditorSetStatuses(new StatusApplication { type = StatusEffectType.Slow, duration = 2.5f, magnitude = 1.2f });
            Wire(drift, projectile, hitbox, overlay);
            Wire(cinder, projectile, hitbox, overlay);

            // Tideglass
            var ripple = MakeAbility("TideglassRipple", ContentIdDefaults.AbilityTideglassRipple, "Tideglass Ripple",
                AbilityKind.MeleeHitbox, 0.6f, 14f, 4f, 16, ContentIdDefaults.SchoolTideglass, MagicElement.Tide);
            ripple.EditorSetStatuses(new StatusApplication { type = StatusEffectType.Slow, duration = 2f, magnitude = 1.5f });
            var harpoon = MakeAbility("TideglassHarpoon", ContentIdDefaults.AbilityTideglassHarpoon, "Tideglass Harpoon",
                AbilityKind.Projectile, 0.5f, 16f, 5f, 18, ContentIdDefaults.SchoolTideglass, MagicElement.Tide);
            Wire(ripple, projectile, hitbox, overlay);
            Wire(harpoon, projectile, hitbox, overlay);

            // Continuum (capital)
            var slip = MakeAbility("ContinuumSlip", ContentIdDefaults.AbilityContinuumSlip, "Continuum Slip",
                AbilityKind.Dash, 0.75f, 0f, 0f, 25, ContentIdDefaults.SchoolContinuum, MagicElement.Temporal);
            var echo = MakeAbility("ContinuumEcho", ContentIdDefaults.AbilityContinuumEcho, "Continuum Echo",
                AbilityKind.Projectile, 0.55f, 11f, 2f, 28, ContentIdDefaults.SchoolContinuum, MagicElement.Temporal);
            echo.EditorSetStatuses(new StatusApplication { type = StatusEffectType.Ward, duration = 4f, magnitude = 25f });
            // Ward on self via caster-side would be better; for Stage 2 apply on hit enemy as stolen time ward on caster handled in future.
            Wire(slip, projectile, hitbox, overlay);
            Wire(echo, projectile, hitbox, overlay);

            MakeSchool(ContentIdDefaults.SchoolNeutral, "Neutral Arcana", "Baseline Magus craft.",
                new Color(0.7f, 0.5f, 0.95f),
                new[] { ContentIdDefaults.AbilityBasicBolt, ContentIdDefaults.AbilityArcanePulse, ContentIdDefaults.AbilityBlinkStep });
            MakeSchool(ContentIdDefaults.SchoolGilded, "Gilded Ward", "Forge-fire of the golden city.",
                new Color(1f, 0.75f, 0.25f),
                new[] { ContentIdDefaults.AbilityGildedFlare, ContentIdDefaults.AbilityGildedSmite });
            MakeSchool(ContentIdDefaults.SchoolAshen, "Ashen Veil", "Cinder winds and quiet ruin.",
                new Color(0.55f, 0.5f, 0.55f),
                new[] { ContentIdDefaults.AbilityAshenDrift, ContentIdDefaults.AbilityAshenCinder });
            MakeSchool(ContentIdDefaults.SchoolTideglass, "Tideglass", "Mirrored tides of Neutral Reach.",
                new Color(0.3f, 0.65f, 0.9f),
                new[] { ContentIdDefaults.AbilityTideglassRipple, ContentIdDefaults.AbilityTideglassHarpoon });
            MakeSchool(ContentIdDefaults.SchoolContinuum, "Continuum", "Capital chronomancy.",
                new Color(0.75f, 0.45f, 1f),
                new[] { ContentIdDefaults.AbilityContinuumSlip, ContentIdDefaults.AbilityContinuumEcho });

            ConfigureCity("SampleCity", ContentIdDefaults.CityStarter, "Starter Reach", false, 2, ContentIdDefaults.SchoolTideglass);
            ConfigureCity("GildedWard", ContentIdDefaults.CityGildedWard, "Gilded Ward", false, 2, ContentIdDefaults.SchoolGilded);
            ConfigureCity("AshenQuay", ContentIdDefaults.CityAshenQuay, "Ashen Quay", false, 2, ContentIdDefaults.SchoolAshen);
            ConfigureCity("Capital", ContentIdDefaults.CityCapital, "The Capital", true, 1, ContentIdDefaults.SchoolContinuum);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RealmShards] Magic schools + city definitions ready.");
        }

        private static AbilityDefinition MakeAbility(
            string file, string id, string display, AbilityKind kind,
            float cd, float dmg, float kb, int cost, string school, MagicElement elem)
        {
            string path = $"Assets/Game/Data/Abilities/{file}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorConfigure(id, display, kind, cd, dmg, kb, cost, school, elem);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void Wire(AbilityDefinition a, GameObject p, GameObject h, GameObject o)
        {
            if (a == null) return;
            a.SetPrefabs(p, h, o);
            EditorUtility.SetDirty(a);
        }

        private static void MakeSchool(string id, string name, string desc, Color accent, string[] abilities)
        {
            string path = $"Assets/Game/Data/Magic/{name.Replace(" ", "")}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<MagicSchoolDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MagicSchoolDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorConfigure(id, name, desc, accent, abilities);
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureCity(string file, string id, string name, bool capital, int rooms, string school)
        {
            string path = $"Assets/Game/Data/Cities/{file}.asset";
            var city = AssetDatabase.LoadAssetAtPath<Runs.CityDefinition>(path);
            if (city == null)
            {
                city = ScriptableObject.CreateInstance<Runs.CityDefinition>();
                AssetDatabase.CreateAsset(city, path);
            }

            city.EditorConfigure(id, name, capital, rooms, school);
            EditorUtility.SetDirty(city);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
