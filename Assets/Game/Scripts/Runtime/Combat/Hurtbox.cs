using UnityEngine;

namespace RealmShards
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private FactionMember factionMember;

        public Health Health => health;
        public FactionMember FactionMember => factionMember;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponentInParent<Health>();
            }

            if (factionMember == null)
            {
                factionMember = GetComponentInParent<FactionMember>();
            }

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public bool TryReceiveHit(in DamageInfo damage)
        {
            if (health == null)
            {
                return false;
            }

            return health.TryApplyDamage(in damage);
        }
    }
}
