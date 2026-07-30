using UnityEngine;

namespace RealmShards.Enemies
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.5f;

        private Rigidbody2D _rb;
        private Vector2 _desired;
        private bool _locked;

        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;
        public Vector2 Facing { get; private set; } = Vector2.right;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        public void SetMoveSpeed(float speed) => moveSpeed = Mathf.Max(0f, speed);

        public void SetDesiredVelocity(Vector2 desired)
        {
            if (_locked)
            {
                _desired = Vector2.zero;
                return;
            }

            _desired = desired;
            if (desired.sqrMagnitude > 0.0001f)
                Facing = desired.normalized;
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
                Facing = direction.normalized;
        }

        public void LockMovement(bool locked)
        {
            _locked = locked;
            if (locked && _rb != null)
                _rb.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (_rb == null)
                return;

            if (_locked)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 v = _desired;
            if (v.sqrMagnitude > 1f)
                v.Normalize();

            _rb.linearVelocity = v * moveSpeed;
        }
    }
}
