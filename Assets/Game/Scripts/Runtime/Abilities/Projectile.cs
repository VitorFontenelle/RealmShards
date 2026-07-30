using UnityEngine;

namespace RealmShards
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float knockback = 2.5f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 2.5f;
        [SerializeField] private bool pierce;
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D _body;
        private PrefabPool _pool;
        private FactionMember _owner;
        private float _timer;
        private bool _active;
        private System.Action<DamageInfo, Health> _onHit;
        private readonly System.Collections.Generic.HashSet<int> _hitIds = new System.Collections.Generic.HashSet<int>();

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (_body == null)
            {
                _body = gameObject.AddComponent<Rigidbody2D>();
            }

            _body.gravityScale = 0f;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            CombatLayers.TrySetLayer(gameObject, CombatLayers.Projectile);
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Despawn();
            }
        }

        private void FixedUpdate()
        {
            if (!_active || _body == null)
            {
                return;
            }

            _body.linearVelocity = transform.right * speed;
        }

        public void OnSpawned(PrefabPool pool)
        {
            _pool = pool;
        }

        public void OnDespawned()
        {
            _active = false;
            _hitIds.Clear();
            _owner = null;
            _onHit = null;
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
            }
        }

        public void Launch(
            Vector2 position,
            Vector2 direction,
            FactionMember owner,
            float damageAmount,
            float knockbackForce,
            float moveSpeed,
            float life,
            bool canPierce,
            Color tint,
            System.Action<DamageInfo, Health> onHit = null)
        {
            transform.position = position;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _owner = owner;
            damage = damageAmount;
            knockback = knockbackForce;
            speed = moveSpeed;
            lifetime = life;
            pierce = canPierce;
            _onHit = onHit;
            _timer = lifetime;
            _hitIds.Clear();
            _active = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = tint;
            }

            gameObject.SetActive(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active)
            {
                return;
            }

            var hurtbox = other.GetComponent<Hurtbox>() ?? other.GetComponentInParent<Hurtbox>();
            if (hurtbox != null && hurtbox.Health != null)
            {
                int id = hurtbox.Health.GetEntityId().GetHashCode();
                if (_hitIds.Contains(id))
                {
                    return;
                }

                var targetFaction = hurtbox.FactionMember ?? hurtbox.Health.GetComponent<FactionMember>();
                if (_owner != null && targetFaction != null && !_owner.CanHarm(targetFaction))
                {
                    return;
                }

                Vector2 dir = transform.right;
                var info = DamageInfo.Create(
                    damage,
                    dir * knockback,
                    other.ClosestPoint(transform.position),
                    _owner,
                    _owner != null ? _owner.gameObject : gameObject);

                if (hurtbox.TryReceiveHit(in info))
                {
                    _hitIds.Add(id);
                    _onHit?.Invoke(info, hurtbox.Health);
                    if (!pierce)
                    {
                        Despawn();
                    }
                }

                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            if (_owner != null &&
                damageable.Faction == _owner.Faction &&
                damageable.TeamId == _owner.TeamId &&
                !_owner.FriendlyFire)
            {
                return;
            }

            int did = damageable is MonoBehaviour mb ? mb.GetEntityId().GetHashCode() : damageable.GetHashCode();
            if (_hitIds.Contains(did))
            {
                return;
            }

            var dmgInfo = DamageInfo.Create(
                damage,
                (Vector2)transform.right * knockback,
                other.ClosestPoint(transform.position),
                _owner,
                _owner != null ? _owner.gameObject : gameObject);

            if (damageable.TryApplyDamage(in dmgInfo))
            {
                _hitIds.Add(did);
                if (!pierce)
                {
                    Despawn();
                }
            }
        }

        private void Despawn()
        {
            _active = false;
            if (_pool != null)
            {
                _pool.Release(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
