using UnityEngine;

namespace RealmShards
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private Rigidbody2D body;

        private Vector2 _moveInput;
        private float _speedBonus;
        private bool _castLocked;
        private bool _dashing;
        private bool _disabled;

        public float MoveSpeed => moveSpeed + _speedBonus;
        public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;
        public bool IsMoving => !_castLocked && !_dashing && _moveInput.sqrMagnitude > 0.01f;
        public Vector2 MoveInput => _moveInput;

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FixedUpdate()
        {
            if (_disabled || _dashing)
            {
                return;
            }

            if (_castLocked)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 dir = _moveInput;
            if (dir.sqrMagnitude > 1f)
            {
                dir.Normalize();
            }

            body.linearVelocity = dir * MoveSpeed;
        }

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        public void SetCastLocked(bool locked)
        {
            _castLocked = locked;
            if (locked && body != null && !_dashing)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void SetDashing(bool dashing)
        {
            _dashing = dashing;
            if (dashing && body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void SetDisabled(bool disabled)
        {
            _disabled = disabled;
            if (disabled && body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void AddMoveSpeedBonus(float amount)
        {
            _speedBonus += amount;
        }

        public void ConfigureSpeed(float speed)
        {
            moveSpeed = speed;
        }
    }
}
