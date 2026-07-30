using UnityEngine;

namespace RealmShards.Enemies
{
    public static class EnemyFactory
    {
        public const string KnightSheet = "Assets/Characters/Enemies/knight-spritesheet.png";
        public const string ArcherSheet = "Assets/Characters/Enemies/archer-spritesheet.png";

        public static EnemyBrainBase Spawn(
            EnemyArchetype archetype,
            EnemyDefinition definition,
            Vector3 position,
            float healthMul,
            float damageMul)
        {
            if (definition != null && definition.PrefabOverride != null)
            {
                var instance = Object.Instantiate(definition.PrefabOverride, position, Quaternion.identity);
                var brain = instance.GetComponent<EnemyBrainBase>();
                if (brain != null)
                {
                    brain.Initialize(definition, healthMul, damageMul);
                    return brain;
                }
            }

            return CreateRuntime(archetype, definition, position, healthMul, damageMul);
        }

        public static EnemyBrainBase CreateRuntime(
            EnemyArchetype archetype,
            EnemyDefinition definition,
            Vector3 position,
            float healthMul,
            float damageMul)
        {
            string name = definition != null ? definition.DisplayName : archetype.ToString();
            var go = new GameObject(name);
            go.transform.position = position;
            go.layer = Core.GameLayers.Enemy;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var body = go.AddComponent<CircleCollider2D>();
            body.radius = 0.45f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = Core.SortingLayers.Characters;
            sr.sortingOrder = 10;

            var faction = go.AddComponent<FactionMember>();
            faction.Configure(FactionId.Enemy, 0);

            var health = go.AddComponent<Health>();
            go.AddComponent<EnemyMotor>();
            go.AddComponent<EnemySpriteAnimator>();

            // Hurtbox child so player hitboxes can connect.
            var hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(go.transform);
            hurtGo.transform.localPosition = Vector3.zero;
            hurtGo.layer = Core.GameLayers.Enemy;
            var hurtCol = hurtGo.AddComponent<CircleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.radius = 0.5f;
            var hurtbox = hurtGo.AddComponent<Hurtbox>();

            EnemyBrainBase brain;
            EnemyDefinition def = definition ?? CreateDefaultDefinition(archetype);

            switch (archetype)
            {
                case EnemyArchetype.Archer:
                    brain = go.AddComponent<GoldenArcher>();
                    break;
                case EnemyArchetype.Champion:
                    brain = go.AddComponent<ArcaneCoreChampion>();
                    break;
                default:
                    brain = go.AddComponent<GoldenAxeWarrior>();
                    break;
            }

            brain.Initialize(def, healthMul, damageMul);
            return brain;
        }

        public static EnemyDefinition CreateDefaultDefinition(EnemyArchetype archetype)
        {
            var def = ScriptableObject.CreateInstance<EnemyDefinition>();
            switch (archetype)
            {
                case EnemyArchetype.Archer:
                    def.ApplyRuntimeDefaults("Golden Archer", EnemyArchetype.Archer, 28f, 2.1f, ArcherSheet,
                        new Color(1f, 0.82f, 0.25f));
                    def.ConfigureCombat(0, 6, 24, 6, 9f, 5.5f, 7f, 1.4f, 0.55f);
                    break;
                case EnemyArchetype.Champion:
                    def.ApplyRuntimeDefaults("Arcane Core Champion", EnemyArchetype.Champion, 160f, 2.0f, KnightSheet,
                        new Color(0.75f, 0.45f, 1f));
                    def.ConfigureCombat(0, 6, 20, 6, 1.5f, 0f, 14f, 1.0f, 0.55f, 1.1f);
                    break;
                default:
                    def.ApplyRuntimeDefaults("Golden Axe Warrior", EnemyArchetype.Warrior, 45f, 2.5f, KnightSheet,
                        new Color(1f, 0.78f, 0.2f));
                    def.ConfigureCombat(0, 6, 20, 6, 1.35f, 0f, 10f, 1.1f, 0.45f);
                    break;
            }

            return def;
        }
    }
}
