using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Orchestrates motor/aim/anim and reacts to death.
    /// </summary>
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private DirectionalSpriteAnimator animator;
        [SerializeField] private Health health;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private PlayerIdentity identity;
        [SerializeField] private Collider2D bodyCollider;
        [SerializeField] private GameObject visualRoot;

        private void Awake()
        {
            CacheRefs();
            CombatLayers.TrySetLayer(gameObject, CombatLayers.Player);

            if (bodyCollider == null)
            {
                bodyCollider = GetComponent<Collider2D>();
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void LateUpdate()
        {
            // While moving, facing must follow movement keys (8-dir), not mouse aim.
            // Aim still drives casting / idle look when standing still.
            if (motor != null && motor.IsMoving && motor.MoveInput.sqrMagnitude > 0.01f)
            {
                animator?.SetFacingFromVector(motor.MoveInput);
            }
            else if (aim != null)
            {
                animator?.SetFacingFromVector(aim.AimDirection);
            }

            bool moving = motor != null && motor.IsMoving;
            animator?.SetMoving(moving);
        }

        public void InitializePlayer(int playerIndex, Color? color = null)
        {
            CacheRefs();
            identity?.Setup(playerIndex, color);
            abilityCaster?.SetProjectileTint(identity != null ? identity.PlayerColor : Color.white);

            if (TryGetComponent<FactionMember>(out var faction))
            {
                faction.Configure(FactionId.Player, playerIndex, allowFriendlyFire: false);
            }

            if (GetComponent<Combat.PlayerTargetProxy>() == null)
            {
                gameObject.AddComponent<Combat.PlayerTargetProxy>();
            }

            try
            {
                gameObject.tag = "Player";
            }
            catch
            {
                // Tag may be missing until foundation setup runs.
            }

            CombatLayers.TrySetLayer(gameObject, CombatLayers.Player);
        }

        private void OnDied(Health h)
        {
            motor?.SetDisabled(true);
            abilityCaster?.CancelCast();
            if (bodyCollider != null)
            {
                bodyCollider.enabled = false;
            }
        }

        private void CacheRefs()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (aim == null) aim = GetComponent<PlayerAim>();
            if (animator == null) animator = GetComponentInChildren<DirectionalSpriteAnimator>();
            if (health == null) health = GetComponent<Health>();
            if (abilityCaster == null) abilityCaster = GetComponent<AbilityCaster>();
            if (identity == null) identity = GetComponent<PlayerIdentity>();
        }
    }
}
