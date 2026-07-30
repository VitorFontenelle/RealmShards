using UnityEngine;

namespace RealmShards.Enemies
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private float damage = 6f;
        [SerializeField] private float knockback = 2f;

        private Vector2 _dir;
        private float _spawnTime;
        private GameObject _owner;
        private FactionMember _ownerFaction;
        private bool _alive;
        private SpriteRenderer _sr;
        private CircleCollider2D _col;
        private Rigidbody2D _rb;

        public void Initialize(
            Vector2 direction,
            float spd,
            float life,
            float dmg,
            GameObject owner,
            FactionMember ownerFaction,
            Sprite sprite,
            Color color)
        {
            EnsureComponents();
            _dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            speed = spd;
            lifetime = life;
            damage = dmg;
            _owner = owner;
            _ownerFaction = ownerFaction;
            _spawnTime = Time.time;
            _alive = true;

            _sr.sprite = sprite;
            // Real arrow art should keep its palette; only tint placeholder squares.
            bool placeholder = sprite == null || sprite.texture == null ||
                               (sprite.texture.width <= 16 && sprite.texture.height <= 16 && sprite.rect.width <= 16);
            _sr.color = placeholder ? color : Color.white;
            _sr.sortingLayerName = Core.SortingLayers.SkillEffectsFront;

            // Normalize arrow size — sheet frames vary widely.
            if (sprite != null && !placeholder)
            {
                float len = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                float target = 0.55f;
                float s = len > 0.01f ? target / len : 1f;
                transform.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                transform.localScale = Vector3.one;
            }

            float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            gameObject.SetActive(true);
            _col.enabled = true;
            _rb.linearVelocity = _dir * speed;
        }

        public void Shutdown()
        {
            _alive = false;
            if (_rb != null)
                _rb.linearVelocity = Vector2.zero;
            if (_col != null)
                _col.enabled = false;
            gameObject.SetActive(false);
        }

        private void EnsureComponents()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_col == null) _col = GetComponent<CircleCollider2D>();
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (!_alive)
                return;
            if (Time.time - _spawnTime >= lifetime)
                ProjectilePool.Despawn(this);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_alive || other == null)
                return;

            if (_owner != null && (other.transform == _owner.transform || other.transform.IsChildOf(_owner.transform)))
                return;

            var hurtbox = other.GetComponent<Hurtbox>() ?? other.GetComponentInParent<Hurtbox>();
            IDamageable damageable = hurtbox != null
                ? hurtbox.Health
                : other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
            {
                if (!other.isTrigger)
                    ProjectilePool.Despawn(this);
                return;
            }

            if (damageable.Faction == FactionId.Enemy)
                return;

            Vector2 dir = _dir;
            var info = DamageInfo.Create(
                damage,
                dir * knockback,
                other.ClosestPoint(transform.position),
                _ownerFaction,
                _owner);

            bool hit = hurtbox != null ? hurtbox.TryReceiveHit(in info) : damageable.TryApplyDamage(in info);
            if (hit)
                ProjectilePool.Despawn(this);
        }
    }
}
