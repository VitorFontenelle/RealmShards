using UnityEngine;

namespace RealmShards
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float bobAmplitude = 0.08f;
        [SerializeField] private float bobSpeed = 3f;

        private Vector3 _origin;
        private bool _collected;

        public ItemDefinition Item => item;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
            CombatLayers.TrySetLayer(gameObject, CombatLayers.Pickup);
            _origin = transform.position;
            ApplyVisual();
        }

        private void Update()
        {
            transform.position = _origin + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
        }

        public void Setup(ItemDefinition definition)
        {
            item = definition;
            _origin = transform.position;
            _collected = false;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null || item == null)
            {
                return;
            }

            if (item.Icon != null)
            {
                spriteRenderer.sprite = item.Icon;
            }

            spriteRenderer.color = item.Tint;
        }

        public bool TryCollect(PlayerInventory inventory)
        {
            if (_collected || inventory == null || item == null)
            {
                return false;
            }

            if (!inventory.TryAdd(item))
            {
                return false;
            }

            _collected = true;
            Destroy(gameObject);
            return true;
        }
    }
}
