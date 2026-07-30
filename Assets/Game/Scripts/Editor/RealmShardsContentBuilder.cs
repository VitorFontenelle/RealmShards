#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards.Editor
{
    public static class RealmShardsContentBuilder
    {
        private const string PlayerPrefabPath = "Assets/Game/Prefabs/Characters/Player.prefab";
        private const string ProjectilePrefabPath = "Assets/Game/Prefabs/Projectiles/ArcaneBolt.prefab";
        private const string HitboxPrefabPath = "Assets/Game/Prefabs/Combat/MeleeHitbox.prefab";
        private const string OverlayPrefabPath = "Assets/Game/Prefabs/Combat/CastOverlay.prefab";
        private const string PickupPrefabPath = "Assets/Game/Prefabs/Pickups/ItemPickup.prefab";
        private const string DummyPrefabPath = "Assets/Game/Prefabs/Combat/TrainingDummy.prefab";
        private const string AnimSetPath = "Assets/Game/Data/Animation/MagusDirectionalSet.asset";
        private const string InputActionsPath = "Assets/Game/Settings/RealmShards.inputactions";
        private const string MaterialPath = "Assets/Game/Materials/PlayerTint.mat";
        private const string MarkerPath = "Assets/Game/Data/.realmshards_player_setup";

        [MenuItem("RealmShards/Setup Player Content")]
        public static void BuildAllMenu()
        {
            BuildAll(force: true);
        }

        [InitializeOnLoadMethod]
        private static void AutoSetupIfNeeded()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (!File.Exists(PlayerPrefabPath) || !File.Exists(AnimSetPath))
                {
                    BuildAll(force: false);
                }
            };
        }

        public static void BuildAll(bool force)
        {
            EnsureFolders();
            EnsureLayers();

            var animSet = BuildAnimationSet();
            var material = BuildMaterial();
            var projectile = BuildProjectilePrefab();
            var hitbox = BuildHitboxPrefab();
            var overlay = BuildOverlayPrefab();
            var pickup = BuildPickupPrefab();
            var bolt = BuildAbility("ArcaneBolt", AbilityKind.Projectile, 0.35f, 14f, 2.5f);
            var pulse = BuildAbility("ArcanePulse", AbilityKind.MeleeHitbox, 0.55f, 18f, 5f);
            var blink = BuildAbility("BlinkStep", AbilityKind.Dash, 0.85f, 0f, 0f);
            TuneAbility(bolt, windup: 0.05f, active: 0.05f, recovery: 0.1f, range: 9f, speed: 13f);
            TuneAbility(pulse, windup: 0.08f, active: 0.12f, recovery: 0.18f, hitDistance: 0.8f, hitRadius: 0.95f);
            TuneAbility(blink, windup: 0.02f, active: 0.12f, recovery: 0.08f, dashDistance: 3.4f, dashDuration: 0.12f);
            ConfigureAbilityPrefabs(bolt, projectile, hitbox, overlay);
            ConfigureAbilityPrefabs(pulse, projectile, hitbox, overlay);
            ConfigureAbilityPrefabs(blink, projectile, hitbox, overlay);

            var vitalityCharm = BuildItem("VitalityCharm", ItemKind.StatBoost, "Raises max health and move speed.",
                new Color(0.95f, 0.4f, 0.45f));
            SetItemStats(vitalityCharm, maxHealth: 25f, moveSpeed: 0.4f);

            var sparkRelic = BuildItem("SparkRelic", ItemKind.EventTrigger, "Instant heal and short i-frames.",
                new Color(1f, 0.9f, 0.35f));
            SetItemEvent(sparkRelic, heal: 999f, iframes: true, iframeTime: 1.25f);

            var focusBand = BuildItem("FocusBand", ItemKind.AbilityModifier, "Slight mobility focus after pickup.",
                new Color(0.45f, 0.75f, 1f));

            var player = BuildPlayerPrefab(animSet, material, bolt, pulse, bolt, blink, projectile, hitbox, overlay, pickup);
            BuildDummyPrefab();
            BuildSamplePickups(pickup, vitalityCharm, sparkRelic, focusBand);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.WriteAllText(MarkerPath, System.DateTime.UtcNow.ToString("O"));
            Debug.Log("[RealmShards] Player content setup complete. Prefab: " + PlayerPrefabPath);
            _ = force;
            _ = player;
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/Game",
                "Assets/Game/Data",
                "Assets/Game/Data/Abilities",
                "Assets/Game/Data/Items",
                "Assets/Game/Data/Animation",
                "Assets/Game/Prefabs",
                "Assets/Game/Prefabs/Characters",
                "Assets/Game/Prefabs/Combat",
                "Assets/Game/Prefabs/Projectiles",
                "Assets/Game/Prefabs/Pickups",
                "Assets/Game/Materials",
                "Assets/Game/Settings",
                "Assets/Game/Shaders"
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                    var name = Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                    {
                        AssetDatabase.CreateFolder(parent, name);
                    }
                }
            }
        }

        private static void EnsureLayers()
        {
            AddLayer("Player");
            AddLayer("Enemy");
            AddLayer("PlayerProjectile");
            AddLayer("EnemyProjectile");
            AddLayer("PlayerHitbox");
            AddLayer("EnemyHitbox");
            AddLayer("Pickup");
            AddLayer("Environment");
            AddLayer("Trigger");
            AddLayer("RoomBoundary");
            AddLayer("Projectile");
        }

        private static void AddLayer(string layerName)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName)
                {
                    return;
                }
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
        }

        private static DirectionalAnimationSet BuildAnimationSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<DirectionalAnimationSet>(AnimSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<DirectionalAnimationSet>();
                AssetDatabase.CreateAsset(set, AnimSetPath);
            }

            var idle = LoadSprites("Assets/Characters/Magus/standing.png").FirstOrDefault();
            var run = LoadSprites("Assets/Characters/Magus/running-spritesheet.png")
                .OrderBy(s => ExtractIndex(s.name))
                .ToArray();
            var cast = LoadSprites("Assets/Characters/Magus/attacking-spritesheet.png")
                .OrderBy(s => ExtractIndex(s.name))
                .ToArray();

            set.SetSprites(idle, run, cast);
            EditorUtility.SetDirty(set);
            return set;
        }

        private static Sprite[] LoadSprites(string texturePath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
            var list = new List<Sprite>();
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    list.Add(sprite);
                }
            }

            return list.ToArray();
        }

        private static int ExtractIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            if (underscore < 0)
            {
                return 0;
            }

            return int.TryParse(name[(underscore + 1)..], out int value) ? value : 0;
        }

        private static Material BuildMaterial()
        {
            var shader = Shader.Find("RealmShards/SpriteTintRecolor");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            else
            {
                mat.shader = shader;
            }

            mat.SetColor("_Color", new Color(0.72f, 0.45f, 0.95f, 1f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static AbilityDefinition BuildAbility(string name, AbilityKind kind, float cd, float dmg, float kb)
        {
            string path = $"Assets/Game/Data/Abilities/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<AbilityDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorConfigure(name, kind, cd, dmg, kb);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void ConfigureAbilityPrefabs(
            AbilityDefinition ability,
            GameObject projectile,
            GameObject hitbox,
            GameObject overlay)
        {
            ability.SetPrefabs(projectile, hitbox, overlay);
            EditorUtility.SetDirty(ability);
        }

        private static void TuneAbility(
            AbilityDefinition ability,
            float windup = 0.05f,
            float active = 0.1f,
            float recovery = 0.15f,
            float range = 8f,
            float speed = 12f,
            float hitDistance = 0.75f,
            float hitRadius = 0.85f,
            float dashDistance = 3.25f,
            float dashDuration = 0.12f)
        {
            var so = new SerializedObject(ability);
            so.FindProperty("windup").floatValue = windup;
            so.FindProperty("activeDuration").floatValue = active;
            so.FindProperty("recovery").floatValue = recovery;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("projectileSpeed").floatValue = speed;
            so.FindProperty("hitboxDistance").floatValue = hitDistance;
            so.FindProperty("hitboxRadius").floatValue = hitRadius;
            so.FindProperty("dashDistance").floatValue = dashDistance;
            so.FindProperty("dashDuration").floatValue = dashDuration;
            so.FindProperty("castLockMovement").floatValue = windup + active * 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ability);
        }

        private static ItemDefinition BuildItem(string name, ItemKind kind, string desc, Color tint)
        {
            string path = $"Assets/Game/Data/Items/{name}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorConfigure(name, kind, desc);
            var so = new SerializedObject(asset);
            so.FindProperty("tint").colorValue = tint;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetItemStats(ItemDefinition item, float maxHealth, float moveSpeed)
        {
            var so = new SerializedObject(item);
            so.FindProperty("maxHealthBonus").floatValue = maxHealth;
            so.FindProperty("moveSpeedBonus").floatValue = moveSpeed;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void SetItemEvent(ItemDefinition item, float heal, bool iframes, float iframeTime)
        {
            var so = new SerializedObject(item);
            so.FindProperty("healAmount").floatValue = heal;
            so.FindProperty("grantIFrames").boolValue = iframes;
            so.FindProperty("iFrameDuration").floatValue = iframeTime;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static GameObject BuildProjectilePrefab()
        {
            var root = new GameObject("ArcaneBolt");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = new Color(0.7f, 0.45f, 1f, 1f);
            sr.sortingOrder = 20;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.12f;

            root.AddComponent<Projectile>();
            CombatLayers.TrySetLayer(root, CombatLayers.Projectile);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildHitboxPrefab()
        {
            var root = new GameObject("MeleeHitbox");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = new Color(0.85f, 0.55f, 1f, 0.35f);
            sr.sortingOrder = 15;

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            root.AddComponent<Hitbox>();
            CombatLayers.TrySetLayer(root, CombatLayers.PlayerHitbox);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, HitboxPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildOverlayPrefab()
        {
            var root = new GameObject("CastOverlay");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = new Color(0.8f, 0.5f, 1f, 0.5f);
            sr.sortingOrder = 25;
            root.transform.localScale = Vector3.one * 0.6f;
            root.AddComponent<AbilityEffectOverlay>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, OverlayPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildPickupPrefab()
        {
            var root = new GameObject("ItemPickup");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.sortingOrder = 5;
            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;
            root.AddComponent<ItemPickup>();
            CombatLayers.TrySetLayer(root, CombatLayers.Pickup);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PickupPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildDummyPrefab()
        {
            var root = new GameObject("TrainingDummy");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = new Color(0.85f, 0.35f, 0.35f);
            sr.sortingOrder = 3;
            root.transform.localScale = Vector3.one * 1.4f;

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 2f;

            var bodyCol = root.AddComponent<CircleCollider2D>();
            bodyCol.radius = 0.35f;

            root.AddComponent<FactionMember>().Configure(FactionId.Enemy, 0);
            root.AddComponent<Health>();
            root.AddComponent<TrainingDummy>();

            CombatLayers.TrySetLayer(root, CombatLayers.Enemy);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, DummyPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void BuildSamplePickups(
            GameObject pickupPrefab,
            ItemDefinition a,
            ItemDefinition b,
            ItemDefinition c)
        {
            CreateConfiguredPickup("Assets/Game/Prefabs/Pickups/VitalityCharmPickup.prefab", pickupPrefab, a);
            CreateConfiguredPickup("Assets/Game/Prefabs/Pickups/SparkRelicPickup.prefab", pickupPrefab, b);
            CreateConfiguredPickup("Assets/Game/Prefabs/Pickups/FocusBandPickup.prefab", pickupPrefab, c);
        }

        private static void CreateConfiguredPickup(string path, GameObject basePrefab, ItemDefinition item)
        {
            var instance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            if (instance == null)
            {
                return;
            }

            var pickup = instance.GetComponent<ItemPickup>();
            pickup?.Setup(item);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        private static GameObject BuildPlayerPrefab(
            DirectionalAnimationSet animSet,
            Material material,
            AbilityDefinition basic,
            AbilityDefinition a1,
            AbilityDefinition a2,
            AbilityDefinition a3,
            GameObject projectile,
            GameObject hitbox,
            GameObject overlay,
            GameObject pickup)
        {
            var root = new GameObject("Player");
            CombatLayers.TrySetLayer(root, CombatLayers.Player);

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var bodyCol = root.AddComponent<CircleCollider2D>();
            bodyCol.radius = 0.28f;
            bodyCol.offset = new Vector2(0f, -0.05f);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = material;
            sr.sortingOrder = 10;
            if (animSet != null && animSet.IdleSprite != null)
            {
                sr.sprite = animSet.IdleSprite;
            }

            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            ring.transform.localScale = new Vector3(0.55f, 0.22f, 1f);
            var ringSr = ring.AddComponent<SpriteRenderer>();
            ringSr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            ringSr.sortingOrder = 9;
            ringSr.color = new Color(0.72f, 0.45f, 0.95f, 0.7f);

            var labelGo = new GameObject("IndexLabel");
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var text = labelGo.AddComponent<TextMesh>();
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 32;
            text.text = "1";

            var hurt = new GameObject("Hurtbox");
            hurt.transform.SetParent(root.transform, false);
            CombatLayers.TrySetLayer(hurt, CombatLayers.Player);
            var hurtCol = hurt.AddComponent<CircleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.radius = 0.3f;

            var faction = root.AddComponent<FactionMember>();
            faction.Configure(FactionId.Player, 0, false);
            var health = root.AddComponent<Health>();
            health.Configure(100f, 0.35f);
            hurt.AddComponent<Hurtbox>();

            var animator = visual.AddComponent<DirectionalSpriteAnimator>();
            var animSo = new SerializedObject(animator);
            animSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            animSo.FindProperty("animationSet").objectReferenceValue = animSet;
            animSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<PlayerMotor>();
            root.AddComponent<PlayerAim>();
            root.AddComponent<PlayerInteractor>();
            var inventory = root.AddComponent<PlayerInventory>();
            inventory.Configure(6, pickup);

            var caster = root.AddComponent<AbilityCaster>();
            caster.ConfigureDefaults(basic, a1, a2, a3, projectile, hitbox, overlay);

            var identity = root.AddComponent<PlayerIdentity>();
            var idSo = new SerializedObject(identity);
            idSo.FindProperty("bodyRenderer").objectReferenceValue = sr;
            idSo.FindProperty("ringRenderer").objectReferenceValue = ringSr;
            idSo.FindProperty("indicatorLabel").objectReferenceValue = text;
            idSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<PlayerController>();
            root.AddComponent<PlayerInputBridge>();

            try
            {
                root.tag = "Player";
            }
            catch
            {
                // ignored
            }

            root.AddComponent<Combat.PlayerTargetProxy>();

            var playerInput = root.AddComponent<PlayerInput>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;
            playerInput.neverAutoSwitchControlSchemes = false;

            var hitboxChild = new GameObject("PlayerHitboxAnchor");
            hitboxChild.transform.SetParent(root.transform, false);
            CombatLayers.TrySetLayer(hitboxChild, CombatLayers.PlayerHitbox);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        [MenuItem("RealmShards/Create Combat Test Scene Objects")]
        public static void CreateCombatTestHelpers()
        {
            BuildAll(force: false);

            var spawnerGo = new GameObject("PlayerJoinSpawner");
            var spawner = spawnerGo.AddComponent<PlayerJoinSpawner>();
            var spawnerSo = new SerializedObject(spawner);
            spawnerSo.FindProperty("playerPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            spawnerSo.FindProperty("spawnPlayerOnStart").boolValue = true;
            spawnerSo.ApplyModifiedPropertiesWithoutUndo();

            if (Object.FindFirstObjectByType<PoolHub>() == null)
            {
                new GameObject("PoolHub").AddComponent<PoolHub>();
            }

            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefabPath);
            if (dummyPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(dummyPrefab);
                var d2 = PrefabUtility.InstantiatePrefab(dummyPrefab) as GameObject;
                if (d2 != null)
                {
                    d2.transform.position = new Vector3(2.5f, 0f, 0f);
                }
            }

            var pickupA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Pickups/VitalityCharmPickup.prefab");
            if (pickupA != null)
            {
                var p = PrefabUtility.InstantiatePrefab(pickupA) as GameObject;
                if (p != null)
                {
                    p.transform.position = new Vector3(-2f, 1f, 0f);
                }
            }

            var managerGo = new GameObject("PlayerInputManager");
            var manager = managerGo.AddComponent<PlayerInputManager>();
            manager.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[RealmShards] Combat test helpers placed in the open scene.");
        }
    }
}
#endif
