using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Drop this in a scene (or use PlayerJoinSpawner). Ensures PoolHub exists.
    /// </summary>
    public sealed class CombatSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private bool createPoolHub = true;

        private void Awake()
        {
            if (createPoolHub && FindFirstObjectByType<PoolHub>() == null)
            {
                var go = new GameObject("PoolHub");
                go.AddComponent<PoolHub>();
            }
        }
    }
}
