using RealmShards.Core;
using UnityEngine;

namespace RealmShards
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float damage = 10f;
        [SerializeField] private float knockback = 2.5f;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 12f;
        [SerializeField] private bool pierce;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float offscreenPadding = 0.12f;

        private Rigidbody2D _body;
        private PrefabPool _pool;
        private FactionMember _owner;
        private float _timer;
        private bool _active;
        private bool _vanishing;
        private float _distanceTraveled;
        private float _maxTravelRange;
        private bool _useRangeLimit;
        private bool _playMissVanish;
        private Vector2 _lastPosition;
        private ProjectileSheetAnimator _sheetAnimator;
        private System.Action<DamageInfo, Health> _onHit;
        private readonly System.Collections.Generic.HashSet<int> _hitIds = new System.Collections.Generic.HashSet<int>();
        private Camera _cam;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            if (_body == null)
                _body = gameObject.AddComponent<Rigidbody2D>();

            _body.gravityScale = 0f;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            _sheetAnimator = GetComponent<ProjectileSheetAnimator>();
            EnsureVisibleSorting();
            CombatLayers.TrySetLayer(gameObject, CombatLayers.Projectile);
        }

        private void Update()
        {
            if (!_active || _vanishing)
                return;

            _sheetAnimator?.TickFlight(Time.deltaTime);

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                BeginMissExpire();
                return;
            }

            if (_useRangeLimit)
            {
                Vector2 pos = transform.position;
                _distanceTraveled += Vector2.Distance(_lastPosition, pos);
                _lastPosition = pos;
                if (_distanceTraveled >= _maxTravelRange)
                    BeginMissExpire();
                return;
            }

            if (IsOffscreen())
                Despawn();
        }

        private void FixedUpdate()
        {
            if (!_active || _vanishing || _body == null)
                return;

            _body.linearVelocity = transform.right * speed;
        }

        public void OnSpawned(PrefabPool pool) => _pool = pool;

        public void OnDespawned()
        {
            _active = false;
            _vanishing = false;
            _hitIds.Clear();
            _owner = null;
            _onHit = null;
            _useRangeLimit = false;
            _playMissVanish = false;
            _distanceTraveled = 0f;
            if (_body != null)
                _body.linearVelocity = Vector2.zero;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
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
            System.Action<DamageInfo, Health> onHit = null,
            float maxTravelRange = 0f,
            bool playMissVanish = false)
        {
            EnsureVisibleSorting();

            transform.position = new Vector3(position.x, position.y, 0f);
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;

            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _owner = owner;
            damage = damageAmount;
            knockback = knockbackForce;
            speed = moveSpeed;
            lifetime = maxTravelRange > 0.01f ? Mathf.Max(life, 2f) : Mathf.Max(life, 8f);
            pierce = canPierce;
            _onHit = onHit;
            _timer = lifetime;
            _hitIds.Clear();
            _active = true;
            _vanishing = false;
            _useRangeLimit = maxTravelRange > 0.01f;
            _maxTravelRange = maxTravelRange;
            _playMissVanish = playMissVanish;
            _distanceTraveled = 0f;
            _lastPosition = position;
            _cam = Camera.main;

            if (spriteRenderer != null)
                spriteRenderer.color = tint;

            _sheetAnimator?.ResetFlight();
            gameObject.SetActive(true);
        }

        private void EnsureVisibleSorting()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                return;

            spriteRenderer.sortingLayerName = SortingLayers.SkillEffectsFront;
            if (spriteRenderer.sortingOrder < 20)
                spriteRenderer.sortingOrder = 20;
        }

        private bool IsOffscreen()
        {
            if (_cam == null)
                _cam = Camera.main;
            if (_cam == null)
                return false;

            Vector3 vp = _cam.WorldToViewportPoint(transform.position);
            float pad = offscreenPadding;
            return vp.z < 0f ||
                   vp.x < -pad || vp.x > 1f + pad ||
                   vp.y < -pad || vp.y > 1f + pad;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active || _vanishing)
                return;

            if (TryHandleEnvironmentHit(other))
                return;

            var hurtbox = other.GetComponent<Hurtbox>() ?? other.GetComponentInParent<Hurtbox>();
            if (hurtbox != null && hurtbox.Health != null)
            {
                int id = hurtbox.Health.GetEntityId().GetHashCode();
                if (_hitIds.Contains(id))
                    return;

                var targetFaction = hurtbox.FactionMember ?? hurtbox.Health.GetComponent<FactionMember>();
                if (_owner != null && targetFaction != null && !_owner.CanHarm(targetFaction))
                    return;

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
                        Despawn();
                }

                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
                return;

            if (_owner != null &&
                damageable.Faction == _owner.Faction &&
                damageable.TeamId == _owner.TeamId &&
                !_owner.FriendlyFire)
            {
                return;
            }

            int did = damageable is MonoBehaviour mb ? mb.GetEntityId().GetHashCode() : damageable.GetHashCode();
            if (_hitIds.Contains(did))
                return;

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
                    Despawn();
            }
        }

        private bool TryHandleEnvironmentHit(Collider2D other)
        {
            if (other.isTrigger)
                return false;
            if (other.GetComponent<Hurtbox>() != null || other.GetComponentInParent<Hurtbox>() != null)
                return false;

            if (other.gameObject.layer != GameLayers.Environment)
                return false;

            BeginMissExpire();
            return true;
        }

        private void BeginMissExpire()
        {
            if (_vanishing)
                return;

            _active = false;
            if (_body != null)
                _body.linearVelocity = Vector2.zero;

            if (_playMissVanish && _sheetAnimator != null && _sheetAnimator.PlayVanish(Despawn))
                _vanishing = true;
            else
                Despawn();
        }

        private void Despawn()
        {
            _active = false;
            _vanishing = false;
            if (_pool != null)
                _pool.Release(gameObject);
            else
                gameObject.SetActive(false);
        }
    }
}
