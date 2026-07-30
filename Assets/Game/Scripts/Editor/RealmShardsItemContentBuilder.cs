using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RealmShards.Editor
{
#if UNITY_EDITOR
    /// <summary>
    /// Regenerates the Stage-2 item catalog (8 buff items + icons).
    /// </summary>
    public static class RealmShardsItemContentBuilder
    {
        private const string ItemsFolder = "Assets/Game/Data/Items";
        private const string IconsFolder = "Assets/Game/Art/Items/Generated";

        [MenuItem("RealmShards/Setup Items Content")]
        public static void BuildItemsMenu()
        {
            BuildAll();
        }

        public static void BuildAll()
        {
            EnsureFolder(ItemsFolder);
            EnsureFolder(IconsFolder);

            var heart = Build(
                "HeartwardAmulet",
                Save.ContentIdDefaults.ItemHeartward,
                "Heartward Amulet",
                ItemKind.StatBoost,
                "A warm pulse steadies the blood.",
                "+25 Max HP and a touch of haste.",
                new Color(0.95f, 0.4f, 0.45f),
                "HeartwardAmulet.png");
            SetStats(heart, maxHp: 25f, move: 0.25f);

            var ember = Build(
                "EmberSparkRelic",
                Save.ContentIdDefaults.ItemEmberSpark,
                "Ember Spark Relic",
                ItemKind.EventTrigger,
                "A captive spark of Gilded Ward forges.",
                "Instant heal and brief invulnerability.",
                new Color(1f, 0.85f, 0.35f),
                "EmberSparkRelic.png");
            SetEvent(ember, heal: 40f, iframes: true, iframeTime: 1.1f);

            var mind = Build(
                "MindthreadBand",
                Save.ContentIdDefaults.ItemMindthread,
                "Mindthread Band",
                ItemKind.AbilityModifier,
                "Woven thoughts shorten the cast.",
                "15% cooldown reduction on all abilities.",
                new Color(0.45f, 0.75f, 1f),
                "MindthreadBand.png");
            SetCooldown(mind, 0.85f);

            var needle = Build(
                "NeedleShard",
                Save.ContentIdDefaults.ItemNeedleShard,
                "Needle Shard",
                ItemKind.AbilityModifier,
                "A splinter of Continuum glass.",
                "Bolts pierce and deal +4 damage.",
                new Color(0.75f, 0.45f, 1f),
                "NeedleShard.png");
            SetBolt(needle, pierce: true, flatDamage: 4f);

            var halo = Build(
                "WideningHalo",
                Save.ContentIdDefaults.ItemWideningHalo,
                "Widening Halo",
                ItemKind.AbilityModifier,
                "Pulse magic spreads like tideglass.",
                "+0.45 Pulse radius.",
                new Color(0.35f, 0.9f, 0.85f),
                "WideningHalo.png");
            SetPulse(halo, radius: 0.45f);

            var stride = Build(
                "Stridefeather",
                Save.ContentIdDefaults.ItemStridefeather,
                "Stridefeather",
                ItemKind.AbilityModifier,
                "Ashen Veil wind underfoot.",
                "+1.2 Blink distance.",
                new Color(0.55f, 0.95f, 0.7f),
                "Stridefeather.png");
            SetBlink(stride, distance: 1.2f);

            var lode = Build(
                "LodestoneCharm",
                Save.ContentIdDefaults.ItemLodestone,
                "Lodestone Charm",
                ItemKind.AbilityModifier,
                "Relics lean toward the bearer.",
                "Pickup magnet radius 3.5.",
                new Color(0.4f, 0.85f, 0.5f),
                "LodestoneCharm.png");
            SetMagnet(lode, radius: 3.5f);

            var chrono = Build(
                "ChronoweaveSand",
                Save.ContentIdDefaults.ItemChronoweave,
                "Chronoweave Sand",
                ItemKind.AbilityModifier,
                "Grains stolen from the Capital clock.",
                "+12% damage; on-hit 8% chance for +1 Vestige crumb.",
                new Color(0.9f, 0.7f, 1f),
                "ChronoweaveSand.png");
            SetOnHit(chrono, damageMult: 0.12f, vestigeChance: 0.08f, vestigeAmt: 1, healOnHit: 0f);

            var iron = Build(
                "IronvineCharm",
                Save.ContentIdDefaults.ItemIronvine,
                "Ironvine Charm",
                ItemKind.StatBoost,
                "Rooted wards of the Reach.",
                "+40 Max HP; slight slow tradeoff (-0.15 move).",
                new Color(0.45f, 0.7f, 0.4f),
                "IronvineCharm.png");
            SetStats(iron, maxHp: 40f, move: -0.15f);

            var glass = Build(
                "GlassmarrowPhial",
                Save.ContentIdDefaults.ItemGlassmarrow,
                "Glassmarrow Phial",
                ItemKind.AbilityModifier,
                "Tideglass marrow for sharper casts.",
                "+2 bolt splits; +6 flat ability damage.",
                new Color(0.55f, 0.85f, 0.95f),
                "GlassmarrowPhial.png");
            SetBolt(glass, pierce: false, flatDamage: 6f);
            var glassSo = new SerializedObject(glass);
            glassSo.FindProperty("boltSplitExtraProjectiles").intValue = 2;
            glassSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(glass);

            BuildPickupPrefabs(heart, ember, mind, needle, halo, stride, lode, chrono, iron, glass);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RealmShards] Items content setup complete (10 items).");
        }

        private static ItemDefinition Build(
            string fileName,
            string id,
            string display,
            ItemKind kind,
            string flavor,
            string desc,
            Color tint,
            string iconFile)
        {
            string path = $"{ItemsFolder}/{fileName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorConfigure(id, display, kind, desc, flavor);
            var so = new SerializedObject(asset);
            so.FindProperty("tint").colorValue = tint;
            so.ApplyModifiedPropertiesWithoutUndo();

            string iconPath = $"{IconsFolder}/{iconFile}";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (tex != null)
            {
                var sprites = AssetDatabase.LoadAllAssetsAtPath(iconPath);
                Sprite sprite = null;
                foreach (var o in sprites)
                {
                    if (o is Sprite s)
                    {
                        sprite = s;
                        break;
                    }
                }

                if (sprite == null)
                {
                    // Force sprite import
                    var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = 64;
                        importer.SaveAndReimport();
                        sprites = AssetDatabase.LoadAllAssetsAtPath(iconPath);
                        foreach (var o in sprites)
                        {
                            if (o is Sprite s)
                            {
                                sprite = s;
                                break;
                            }
                        }
                    }
                }

                if (sprite != null)
                    asset.EditorSetIcon(sprite);
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetStats(ItemDefinition item, float maxHp, float move)
        {
            var so = new SerializedObject(item);
            so.FindProperty("maxHealthBonus").floatValue = maxHp;
            so.FindProperty("moveSpeedBonus").floatValue = move;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetEvent(ItemDefinition item, float heal, bool iframes, float iframeTime)
        {
            var so = new SerializedObject(item);
            so.FindProperty("healAmount").floatValue = heal;
            so.FindProperty("grantIFrames").boolValue = iframes;
            so.FindProperty("iFrameDuration").floatValue = iframeTime;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetCooldown(ItemDefinition item, float mult)
        {
            var so = new SerializedObject(item);
            so.FindProperty("cooldownMultiplier").floatValue = mult;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetBolt(ItemDefinition item, bool pierce, float flatDamage)
        {
            var so = new SerializedObject(item);
            so.FindProperty("grantBoltPierce").boolValue = pierce;
            so.FindProperty("abilityDamageFlatBonus").floatValue = flatDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetPulse(ItemDefinition item, float radius)
        {
            var so = new SerializedObject(item);
            so.FindProperty("pulseRadiusBonus").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetBlink(ItemDefinition item, float distance)
        {
            var so = new SerializedObject(item);
            so.FindProperty("blinkDistanceBonus").floatValue = distance;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetMagnet(ItemDefinition item, float radius)
        {
            var so = new SerializedObject(item);
            so.FindProperty("pickupMagnetRadius").floatValue = radius;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetOnHit(ItemDefinition item, float damageMult, float vestigeChance, int vestigeAmt, float healOnHit)
        {
            var so = new SerializedObject(item);
            so.FindProperty("damageMultiplierBonus").floatValue = damageMult;
            so.FindProperty("onHitVestigeChance").floatValue = vestigeChance;
            so.FindProperty("onHitVestigeAmount").intValue = vestigeAmt;
            so.FindProperty("onHitHeal").floatValue = healOnHit;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void BuildPickupPrefabs(params ItemDefinition[] items)
        {
            var basePickup = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Pickups/ItemPickup.prefab");
            if (basePickup == null)
                return;

            foreach (var item in items)
            {
                if (item == null) continue;
                string path = $"Assets/Game/Prefabs/Pickups/{item.name}Pickup.prefab";
                var instance = PrefabUtility.InstantiatePrefab(basePickup) as GameObject;
                if (instance == null) continue;
                instance.GetComponent<ItemPickup>()?.Setup(item);
                PrefabUtility.SaveAsPrefabAsset(instance, path);
                Object.DestroyImmediate(instance);
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
#endif
}
