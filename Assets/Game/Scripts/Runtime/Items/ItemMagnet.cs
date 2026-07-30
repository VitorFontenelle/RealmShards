using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Pulls nearby pickups toward the player when magnet radius &gt; 0.
    /// </summary>
    public sealed class ItemMagnet : MonoBehaviour
    {
        [SerializeField] private PlayerItemModifiers modifiers;
        [SerializeField] private float pullSpeed = 8f;
        [SerializeField] private float autoPickupRadius = 0.55f;
        [SerializeField] private PlayerInventory inventory;

        private readonly Collider2D[] _hits = new Collider2D[24];

        private void Awake()
        {
            if (modifiers == null) modifiers = GetComponent<PlayerItemModifiers>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
        }

        private void Update()
        {
            float radius = modifiers != null ? modifiers.PickupMagnetRadius : 0f;
            if (radius <= 0.01f)
                return;

            int count = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _hits);
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var pickup = col.GetComponent<ItemPickup>() ?? col.GetComponentInParent<ItemPickup>();
                if (pickup == null) continue;

                Vector2 target = transform.position;
                Vector2 pos = pickup.transform.position;
                float dist = Vector2.Distance(pos, target);
                pickup.transform.position = Vector2.MoveTowards(pos, target, pullSpeed * Time.deltaTime);

                if (dist <= autoPickupRadius && inventory != null)
                    pickup.TryCollect(inventory);
            }
        }
    }
}
