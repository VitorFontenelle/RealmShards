using RealmShards.CameraSystem;
using RealmShards.Core;
using RealmShards.Enemies;
using RealmShards.Input;
using RealmShards.Rooms;
using RealmShards.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards.World
{
    /// <summary>
    /// Boots CityRun: arena, camera, mixed-device players from hub lobby, encounter.
    /// </summary>
    public sealed class CityRunBootstrap : MonoBehaviour, ICityRunReady
    {
        public const string PlayerPrefabPath = "Assets/Game/Prefabs/Characters/Player.prefab";

        [SerializeField] private Vector2 roomSize = new Vector2(24f, 16f);
        [SerializeField] private EncounterDefinition encounterOverride;
        [SerializeField] private CoopScalingConfig coopScalingOverride;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private bool buildArenaIfMissing = true;
        [SerializeField] private bool spawnPlayer = true;
        [SerializeField] private bool warmProjectilePool = true;

        private ArenaBuilder.ArenaResult _arena;

        private void Awake()
        {
            if (FindFirstObjectByType<PoolHub>() == null)
            {
                var hub = new GameObject("PoolHub");
                hub.AddComponent<PoolHub>();
            }

            if (warmProjectilePool)
                ProjectilePool.Warm(16);

            if (buildArenaIfMissing)
                _arena = ArenaBuilder.Build(roomSize, transform);

            SetupCamera();
            if (spawnPlayer)
                SpawnPlayers();

            SetupEncounter();
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
                cam.orthographic = true;
                cam.orthographicSize = 7f;
                cam.backgroundColor = new Color(0.12f, 0.11f, 0.14f, 1f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }

            var shared = cam.GetComponent<SharedOrthoCamera>();
            if (shared == null)
                shared = cam.gameObject.AddComponent<SharedOrthoCamera>();

            Transform fallback = _arena.PlayerSpawn != null ? _arena.PlayerSpawn : transform;
            shared.Configure(_arena.Bounds, fallback, 5f, 11f);
        }

        private void SpawnPlayers()
        {
            GameObject prefab = playerPrefab;
#if UNITY_EDITOR
            if (prefab == null)
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
#endif

            Vector3 basePos = _arena.PlayerSpawn != null ? _arena.PlayerSpawn.position : Vector3.zero;
            var lobby = GameContext.Instance != null ? GameContext.Instance.Lobby : null;
            int count = GameContext.Instance?.RunSession?.LocalPlayerCount ?? 1;
            count = Mathf.Clamp(count, 1, 4);

            bool anyJoined = false;
            if (lobby != null)
            {
                for (int i = 0; i < LocalCoopLobby.MaxPlayers; i++)
                {
                    var slot = lobby.GetSlot(i);
                    if (!slot.Joined) continue;
                    anyJoined = true;
                    SpawnOne(prefab, basePos + new Vector3(i * 1.2f, 0f, 0f), i, slot);
                }
            }

            if (!anyJoined)
            {
                for (int i = 0; i < count; i++)
                    SpawnOne(prefab, basePos + new Vector3(i * 1.2f, 0f, 0f), i, null);
            }
        }

        private void SpawnOne(GameObject prefab, Vector3 pos, int index, LocalCoopLobby.Slot slot)
        {
            if (prefab != null)
            {
                var instance = Instantiate(prefab, pos, Quaternion.identity);
                instance.name = $"Player_{index + 1}";
                try { instance.tag = "Player"; } catch { /* ignore */ }
                instance.GetComponent<PlayerController>()?.InitializePlayer(index);
                LoadoutApplier.ApplyFromSession(instance.GetComponent<AbilityCaster>());
                if (instance.GetComponent<Magic.StatusEffectHost>() == null)
                    instance.AddComponent<Magic.StatusEffectHost>();

                var pi = instance.GetComponent<PlayerInput>();
                if (pi != null && slot != null && slot.PrimaryDevice != null)
                {
                    try
                    {
                        if (slot.SecondaryDevice != null)
                            pi.SwitchCurrentControlScheme(slot.SchemeName, slot.PrimaryDevice, slot.SecondaryDevice);
                        else
                            pi.SwitchCurrentControlScheme(slot.SchemeName, slot.PrimaryDevice);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[CityRun] Control scheme assign failed: {ex.Message}");
                    }
                }

                return;
            }

            if (index == 0)
                CreatePlaceholderPlayer(pos);
        }

        private static void CreatePlaceholderPlayer(Vector3 pos)
        {
            var go = new GameObject("PlayerPlaceholder");
            go.transform.position = pos;
            try { go.tag = "Player"; } catch { /* ignore */ }
            go.layer = GameLayers.Player;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnemySpriteLoader.CreatePlaceholder(new Color(0.3f, 0.75f, 1f), 40);
            sr.sortingLayerName = SortingLayers.Characters;
            sr.sortingOrder = 12;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;

            var faction = go.AddComponent<FactionMember>();
            faction.Configure(FactionId.Player, 0);
            go.AddComponent<Health>().Configure(100f, 0.35f);
            go.AddComponent<PlayerIdentity>().Setup(0);
            go.AddComponent<Combat.PlayerTargetProxy>();

            var hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(go.transform);
            hurtGo.transform.localPosition = Vector3.zero;
            hurtGo.layer = GameLayers.Player;
            var hurtCol = hurtGo.AddComponent<CircleCollider2D>();
            hurtCol.isTrigger = true;
            hurtCol.radius = 0.45f;
            hurtGo.AddComponent<Hurtbox>();

            go.AddComponent<PlaceholderPlayerMover>().Configure(5f);
            go.AddComponent<PlaceholderPlayerAttack>().Configure(14f, 1.1f);
        }

        private void SetupEncounter()
        {
            var encounterGo = new GameObject("EncounterRoom");
            encounterGo.transform.SetParent(transform);
            var room = encounterGo.AddComponent<EncounterRoom>();

            var scaling = coopScalingOverride != null
                ? coopScalingOverride
                : ScriptableObject.CreateInstance<CoopScalingConfig>();

            bool capital = GameContext.Instance?.RunSession?.IsCapitalNode == true;
            EncounterDefinition encounter = encounterOverride;
            if (capital && encounter == null)
            {
                // Capital placeholder: lighter wave, still proves route end.
                encounter = ScriptableObject.CreateInstance<EncounterDefinition>();
                encounter.SetRuntime("capital-gate", new[]
                {
                    new EncounterDefinition.EnemySpawnEntry
                    {
                        archetypeFallback = EnemyArchetype.Warrior,
                        count = 1
                    }
                }, null, true, "capital-clear");
            }

            room.Configure(encounter, scaling, _arena.Bounds, _arena.EnemySpawns, _arena.ChampionSpawns);
            room.SetExitBlockers(_arena.ExitBlockers);
            room.BeginEncounter();
        }
    }

    public sealed class PlaceholderPlayerMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 5f;
        private Rigidbody2D _rb;

        public void Configure(float speed) => moveSpeed = speed;
        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Update()
        {
            float x = 0f, y = 0f;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            }

            Vector2 v = new Vector2(x, y);
            if (v.sqrMagnitude > 1f) v.Normalize();
            if (_rb != null) _rb.linearVelocity = v * moveSpeed;
        }
    }

    public sealed class PlaceholderPlayerAttack : MonoBehaviour
    {
        [SerializeField] private float damage = 14f;
        [SerializeField] private float radius = 1.1f;
        [SerializeField] private float cooldown = 0.35f;
        [SerializeField] private float knockback = 3.5f;

        private float _nextAttackTime;
        private FactionMember _faction;

        public void Configure(float dmg, float range)
        {
            damage = dmg;
            radius = range;
        }

        private void Awake() => _faction = GetComponent<FactionMember>();

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || Time.time < _nextAttackTime) return;
            if (!kb.spaceKey.wasPressedThisFrame && !kb.jKey.wasPressedThisFrame) return;

            _nextAttackTime = Time.time + cooldown;
            Vector2 origin = transform.position;
            var hits = Physics2D.OverlapCircleAll(origin, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                var col = hits[i];
                if (col == null) continue;
                var hurtbox = col.GetComponent<Hurtbox>() ?? col.GetComponentInParent<Hurtbox>();
                IDamageable target = hurtbox != null ? hurtbox.Health : col.GetComponentInParent<IDamageable>();
                if (target == null || !target.IsAlive || target.Faction == FactionId.Player) continue;
                Vector2 dir = ((Vector2)col.bounds.center - origin).normalized;
                var info = DamageInfo.Create(damage, dir * knockback, col.ClosestPoint(origin), _faction, gameObject);
                if (hurtbox != null) hurtbox.TryReceiveHit(in info);
                else target.TryApplyDamage(in info);
            }
        }
    }
}
