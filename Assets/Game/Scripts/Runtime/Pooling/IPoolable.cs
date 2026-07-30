using UnityEngine;

namespace RealmShards
{
    public interface IPoolable
    {
        void OnSpawned(PrefabPool pool);
        void OnDespawned();
    }
}
