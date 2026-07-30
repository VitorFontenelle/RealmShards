using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Thin target handle for AI. Prefer PlayerIdentity + Health when present.
    /// </summary>
    public interface IPlayerMarker
    {
        Transform Transform { get; }
        bool IsAlive { get; }
    }

    /// <summary>
    /// Wraps a player GameObject that already has Health / PlayerIdentity.
    /// </summary>
    public sealed class PlayerTargetAdapter : IPlayerMarker
    {
        private readonly Transform _transform;
        private readonly Health _health;

        public PlayerTargetAdapter(Transform transform, Health health)
        {
            _transform = transform;
            _health = health;
        }

        public Transform Transform => _transform;
        public bool IsAlive => _health == null || _health.IsAlive;
    }
}
