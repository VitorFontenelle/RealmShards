using UnityEngine;

namespace RealmShards.Rooms
{
    /// <summary>
    /// Axis-aligned room bounds used by camera clamp / lock walls.
    /// </summary>
    public sealed class RoomBounds : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(24f, 16f);
        [SerializeField] private Vector2 centerOffset;

        public Vector2 Size => size;
        public Vector3 Center => transform.position + (Vector3)centerOffset;

        public Bounds WorldBounds => new Bounds(Center, new Vector3(size.x, size.y, 1f));

        public void Configure(Vector2 roomSize, Vector2 offset = default)
        {
            size = roomSize;
            centerOffset = offset;
        }

        public Vector3 Clamp(Vector3 worldPos, float padding = 0f)
        {
            var b = WorldBounds;
            float minX = b.min.x + padding;
            float maxX = b.max.x - padding;
            float minY = b.min.y + padding;
            float maxY = b.max.y - padding;
            return new Vector3(
                Mathf.Clamp(worldPos.x, minX, maxX),
                Mathf.Clamp(worldPos.y, minY, maxY),
                worldPos.z);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(Center, new Vector3(size.x, size.y, 0.1f));
        }
    }
}
