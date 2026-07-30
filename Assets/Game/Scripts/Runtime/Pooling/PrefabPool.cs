using System.Collections.Generic;
using UnityEngine;

namespace RealmShards
{
    public sealed class PrefabPool
    {
        private readonly GameObject _prefab;
        private readonly Transform _parent;
        private readonly Stack<GameObject> _available = new Stack<GameObject>();

        public GameObject Prefab => _prefab;

        public PrefabPool(GameObject prefab, Transform parent, int prewarm = 0)
        {
            _prefab = prefab;
            _parent = parent;

            for (int i = 0; i < prewarm; i++)
            {
                var instance = CreateInstance();
                instance.SetActive(false);
                _available.Push(instance);
            }
        }

        public GameObject Get()
        {
            GameObject instance = _available.Count > 0 ? _available.Pop() : CreateInstance();
            instance.SetActive(true);

            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawned(this);
            }

            return instance;
        }

        public T Get<T>() where T : Component
        {
            return Get().GetComponent<T>();
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawned();
            }

            instance.SetActive(false);
            instance.transform.SetParent(_parent, false);
            _available.Push(instance);
        }

        private GameObject CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.name = _prefab.name;
            return instance;
        }
    }
}
