using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Stub trigger revealed after champion defeat. Interact / walk-over logs reward for now.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ArcaneCoreTrigger : MonoBehaviour
    {
        [SerializeField] private string coreId = "arcane-core-stub";
        [SerializeField] private bool consumeOnTouch = true;

        private bool _used;

        public static ArcaneCoreTrigger SpawnStub(Vector3 position)
        {
            var go = new GameObject("ArcaneCore");
            go.transform.position = position;
            go.layer = Core.GameLayers.Trigger;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnemySpriteLoader.CreatePlaceholder(new Color(0.35f, 0.85f, 1f), 48);
            sr.sortingLayerName = Core.SortingLayers.WorldUI;
            sr.sortingOrder = 15;
            sr.color = new Color(0.4f, 0.9f, 1f, 0.95f);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            go.AddComponent<ArcaneCorePulse>();
            return go.AddComponent<ArcaneCoreTrigger>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || other == null)
                return;

            bool isPlayer =
                other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerIdentity>() != null;
            if (!isPlayer)
                return;

            _used = true;
            Debug.Log($"[RealmShards] Arcane Core '{coreId}' activated (stub reward).");
            if (consumeOnTouch)
                Destroy(gameObject, 0.2f);
        }
    }

    public sealed class ArcaneCorePulse : MonoBehaviour
    {
        private Vector3 _baseScale;
        private void Awake() => _baseScale = transform.localScale;
        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * 3f) * 0.12f;
            transform.localScale = _baseScale * s;
        }
    }
}
