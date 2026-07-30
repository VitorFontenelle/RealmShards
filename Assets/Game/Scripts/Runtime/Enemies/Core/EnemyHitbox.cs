using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Damage only while enabled during the configured active hit window (not sprite pixels).
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class EnemyHitbox : MonoBehaviour
    {
        [SerializeField] private float damage = 8f;
        [SerializeField] private float knockback = 3.5f;

        private CircleCollider2D _col;
        private readonly HashSet<int> _hitThisSwing = new HashSet<int>();
        private FactionMember _ownerFaction;
        private GameObject _owner;
        private bool _active;

        private void Awake()
        {
            _col = GetComponent<CircleCollider2D>();
            _col.isTrigger = true;
            _col.enabled = false;
            _owner = transform.root.gameObject;
            _ownerFaction = _owner.GetComponent<FactionMember>();
            gameObject.layer = Core.GameLayers.EnemyHitbox;
        }

        public void Configure(float dmg, float radius, FactionMember ownerFaction = null)
        {
            damage = dmg;
            if (ownerFaction != null)
                _ownerFaction = ownerFaction;
            if (_col == null)
                _col = GetComponent<CircleCollider2D>();
            _col.radius = Mathf.Max(0.05f, radius);
        }

        public void SetActiveWindow(bool active)
        {
            _active = active;
            if (_col == null)
                _col = GetComponent<CircleCollider2D>();
            _col.enabled = active;
            if (active)
                _hitThisSwing.Clear();
        }

        private void OnTriggerStay2D(Collider2D other) => TryHit(other);
        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (!_active || other == null)
                return;

            var hurtbox = other.GetComponent<Hurtbox>() ?? other.GetComponentInParent<Hurtbox>();
            IDamageable damageable = hurtbox != null
                ? hurtbox.Health
                : other.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
                return;

            if (damageable.Faction == FactionId.Enemy)
                return;

            int id = other.GetEntityId().GetHashCode();
            if (damageable is Object uo)
                id = uo.GetEntityId().GetHashCode();
            if (!_hitThisSwing.Add(id))
                return;

            Vector2 dir = ((Vector2)other.bounds.center - (Vector2)transform.position).normalized;
            var info = DamageInfo.Create(
                damage,
                dir * knockback,
                other.ClosestPoint(transform.position),
                _ownerFaction,
                _owner);

            if (hurtbox != null)
                hurtbox.TryReceiveHit(in info);
            else
                damageable.TryApplyDamage(in info);
        }
    }
}
