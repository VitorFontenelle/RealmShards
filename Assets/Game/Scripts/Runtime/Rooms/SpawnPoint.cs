using UnityEngine;

namespace RealmShards.Rooms
{
    public enum SpawnPointKind
    {
        Player,
        Enemy,
        Champion
    }

    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnPointKind kind = SpawnPointKind.Enemy;
        [SerializeField] private string spawnId;
        [SerializeField] private Color gizmoColor = Color.yellow;

        public SpawnPointKind Kind => kind;
        public string SpawnId => spawnId;
        public Vector3 Position => transform.position;

        public void Configure(SpawnPointKind spawnKind, string id = null)
        {
            kind = spawnKind;
            spawnId = id;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                SpawnPointKind.Player => Color.cyan,
                SpawnPointKind.Champion => Color.magenta,
                _ => gizmoColor
            };
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }
    }
}
