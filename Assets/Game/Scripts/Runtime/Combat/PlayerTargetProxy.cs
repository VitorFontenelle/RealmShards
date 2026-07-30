using UnityEngine;

namespace RealmShards.Combat
{
    /// <summary>
    /// Marks a player object for systems that still look up PlayerTargetProxy.
    /// Prefer PlayerIdentity + Health for new code.
    /// </summary>
    public sealed class PlayerTargetProxy : MonoBehaviour, Enemies.IPlayerMarker
    {
        [SerializeField] private Health health;

        public Transform Transform => transform;
        public bool IsAlive => health == null || health.IsAlive;

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }
        }
    }
}
