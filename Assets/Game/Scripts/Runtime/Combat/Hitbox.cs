using System.Collections.Generic;
using UnityEngine;

namespace RealmShards
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hitbox : MonoBehaviour, IPoolable
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float knockbackForce = 4f;
        [SerializeField] private float lifetime = 0.15f;
        [SerializeField] private bool pierce;
        [SerializeField] private FactionMember ownerFaction;

        private readonly HashSet<int> _hitIds = new HashSet<int>();
        private Collider2D _collider;
        private float _timer;
        private bool _active;
        private Transform _follow;
        private Vector2 _localOffset;
        private PrefabPool _pool;

        public float Damage => damage;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
        }

        private void OnDisable()
        {
            _active = false;
            _hitIds.Clear();
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_follow != null)
            {
                transform.position = (Vector2)_follow.position + _localOffset;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Deactivate();
            }
        }

        public void OnSpawned(PrefabPool pool)
        {
            _pool = pool;
        }

        public void OnDespawned()
        {
            _active = false;
            _hitIds.Clear();
            ownerFaction = null;
            _follow = null;
        }

        public void Activate(
            Vector2 position,
            Vector2 direction,
            FactionMember faction,
            float damageAmount,
            float knockback,
            float duration,
            Transform follow = null,
            Vector2 localOffset = default,
            bool canPierce = false)
        {
            transform.position = position;
            if (direction.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            ownerFaction = faction;
            damage = damageAmount;
            knockbackForce = knockback;
            lifetime = duration;
            pierce = canPierce;
            _follow = follow;
            _localOffset = localOffset;
            _timer = lifetime;
            _hitIds.Clear();
            _active = true;
            gameObject.SetActive(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active)
            {
                return;
            }

            TryHit(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_active || pierce)
            {
                return;
            }

            TryHit(other);
        }

        private void TryHit(Collider2D other)
        {
            var hurtbox = other.GetComponent<Hurtbox>() ?? other.GetComponentInParent<Hurtbox>();
            if (hurtbox != null && hurtbox.Health != null)
            {
                int id = hurtbox.Health.GetInstanceID();
                if (_hitIds.Contains(id))
                {
                    return;
                }

                var targetFaction = hurtbox.FactionMember ?? hurtbox.Health.GetComponent<FactionMember>();
                if (ownerFaction != null && targetFaction != null && !ownerFaction.CanHarm(targetFaction))
                {
                    return;
                }

                Vector2 dir = ((Vector2)other.bounds.center - (Vector2)transform.position).normalized;
                if (dir.sqrMagnitude < 0.001f)
                {
                    dir = transform.right;
                }

                var info = DamageInfo.Create(
                    damage,
                    dir * knockbackForce,
                    other.ClosestPoint(transform.position),
                    ownerFaction,
                    ownerFaction != null ? ownerFaction.gameObject : gameObject);

                if (hurtbox.TryReceiveHit(in info))
                {
                    _hitIds.Add(id);
                    if (!pierce)
                    {
                        Deactivate();
                    }
                }

                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            if (ownerFaction != null &&
                damageable.Faction == ownerFaction.Faction &&
                damageable.TeamId == ownerFaction.TeamId &&
                !ownerFaction.FriendlyFire)
            {
                return;
            }

            int did = damageable is MonoBehaviour mb ? mb.GetInstanceID() : damageable.GetHashCode();
            if (_hitIds.Contains(did))
            {
                return;
            }

            Vector2 knockDir = ((Vector2)other.bounds.center - (Vector2)transform.position).normalized;
            if (knockDir.sqrMagnitude < 0.001f)
            {
                knockDir = transform.right;
            }

            var info = DamageInfo.Create(
                damage,
                knockDir * knockbackForce,
                other.ClosestPoint(transform.position),
                ownerFaction,
                ownerFaction != null ? ownerFaction.gameObject : gameObject);

            if (damageable.TryApplyDamage(in info))
            {
                _hitIds.Add(did);
                if (!pierce)
                {
                    Deactivate();
                }
            }
        }

        private void Deactivate()
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
