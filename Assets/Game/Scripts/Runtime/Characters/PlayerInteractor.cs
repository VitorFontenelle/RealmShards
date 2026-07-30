using UnityEngine;

namespace RealmShards
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 1.1f;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private LayerMask pickupMask = ~0;

        private readonly Collider2D[] _hits = new Collider2D[16];

        private void Awake()
        {
            if (inventory == null)
            {
                inventory = GetComponent<PlayerInventory>();
            }
        }

        public void TryInteract()
        {
            if (inventory == null)
            {
                return;
            }

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _hits, pickupMask);
            ItemPickup best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit == null)
                {
                    continue;
                }

                var pickup = hit.GetComponent<ItemPickup>() ?? hit.GetComponentInParent<ItemPickup>();
                if (pickup == null)
                {
                    continue;
                }

                float d = ((Vector2)pickup.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = pickup;
                }
            }

            best?.TryCollect(inventory);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
