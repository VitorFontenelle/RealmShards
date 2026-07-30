using System.Collections.Generic;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Lightweight scene-local pool registry. Not a global GameManager.
    /// </summary>
    public sealed class PoolHub : MonoBehaviour
    {
        private static PoolHub _instance;

        [SerializeField] private int defaultPrewarm = 8;

        private readonly Dictionary<int, PrefabPool> _pools = new Dictionary<int, PrefabPool>();
        private Transform _root;

        public static PoolHub Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PoolHub>();
                    if (_instance == null)
                    {
                        var go = new GameObject("PoolHub");
                        _instance = go.AddComponent<PoolHub>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _root = transform;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public PrefabPool GetPool(GameObject prefab, int prewarm = -1)
        {
            if (prefab == null)
            {
                return null;
            }

            int key = prefab.GetInstanceID();
            if (_pools.TryGetValue(key, out var existing))
            {
                return existing;
            }

            int count = prewarm >= 0 ? prewarm : defaultPrewarm;
            var pool = new PrefabPool(prefab, _root, count);
            _pools[key] = pool;
            return pool;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var pool = GetPool(prefab);
            if (pool == null)
            {
                return null;
            }

            var instance = pool.Get();
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public T Spawn<T>(GameObject prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            var go = Spawn(prefab, position, rotation);
            return go != null ? go.GetComponent<T>() : null;
        }

        public void Despawn(GameObject instance, GameObject prefabKey)
        {
            if (instance == null || prefabKey == null)
            {
                return;
            }

            var pool = GetPool(prefabKey, 0);
            pool?.Release(instance);
        }
    }
}
