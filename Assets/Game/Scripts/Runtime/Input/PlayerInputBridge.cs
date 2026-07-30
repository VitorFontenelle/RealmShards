using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards
{
    /// <summary>
    /// Bridges Input System PlayerInput callbacks to gameplay components.
    /// Uses PlayerInput SendMessages (OnMove, OnBasicAbility, ...) plus per-frame Move/Aim reads.
    /// </summary>
    public sealed class PlayerInputBridge : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerAim aim;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private PlayerInput playerInput;

        private InputAction _move;
        private InputAction _aim;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            CacheAxisActions();
        }

        private void Update()
        {
            if (_move != null && motor != null)
            {
                Vector2 move = _move.ReadValue<Vector2>();
                motor.SetMoveInput(move);
                aim?.SetMoveInput(move);
            }

            if (_aim != null && aim != null)
            {
                bool mouse = playerInput != null &&
                             playerInput.currentControlScheme != null &&
                             playerInput.currentControlScheme.Contains("Keyboard");

                if (mouse && Mouse.current != null)
                {
                    aim.SetMouseScreenPosition(Mouse.current.position.ReadValue());
                }
                else
                {
                    Vector2 stick = _aim.ReadValue<Vector2>();
                    if (stick.sqrMagnitude > 0.01f)
                    {
                        aim.SetAimInput(stick, false);
                    }
                }
            }
        }

        public void OnMove(InputValue value)
        {
            var v = value.Get<Vector2>();
            motor?.SetMoveInput(v);
            aim?.SetMoveInput(v);
        }

        public void OnAim(InputValue value)
        {
            aim?.SetAimInput(value.Get<Vector2>(), false);
        }

        public void OnBasicAbility(InputValue value)
        {
            if (value.isPressed)
            {
                TryCast(0);
            }
        }

        public void OnAbility1(InputValue value)
        {
            if (value.isPressed)
            {
                TryCast(1);
            }
        }

        public void OnAbility2(InputValue value)
        {
            if (value.isPressed)
            {
                TryCast(2);
            }
        }

        public void OnAbility3(InputValue value)
        {
            if (value.isPressed)
            {
                TryCast(3);
            }
        }

        public void OnDash(InputValue value)
        {
            if (value.isPressed)
            {
                TryCastDash();
            }
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed)
            {
                interactor?.TryInteract();
            }
        }

        public void OnDropItem(InputValue value)
        {
            if (value.isPressed)
            {
                inventory?.TryDropLast(out _);
            }
        }

        public void OnPause(InputValue value)
        {
            if (!value.isPressed)
            {
                return;
            }

            Time.timeScale = Time.timeScale > 0f ? 0f : 1f;
        }

        public void OnLocatePlayer(InputValue value)
        {
            if (value.isPressed)
            {
                PlayerLocateSignal.Raise(this);
            }
        }

        public void OnConfirm(InputValue value) { }
        public void OnCancel(InputValue value) { }
        public void OnJoin(InputValue value) { }

        private void TryCast(int slot)
        {
            Vector2 dir = ResolveCastDirection();
            abilityCaster?.TryCast(slot, dir);
        }

        private void TryCastDash()
        {
            if (abilityCaster == null)
            {
                return;
            }

            // Only dash if a Dash ability is actually equipped — never fall back to slot 3.
            for (int i = 0; i < AbilityCaster.SlotCount; i++)
            {
                var def = abilityCaster.GetAbility(i);
                if (def != null && def.Kind == AbilityKind.Dash)
                {
                    TryCast(i);
                    return;
                }
            }
        }

        private Vector2 ResolveCastDirection()
        {
            // Attacks follow the Magus facing (movement look), not mouse aim.
            var animator = GetComponentInChildren<DirectionalSpriteAnimator>();
            if (animator != null)
                return FacingUtility.ToVector(animator.Facing);

            if (motor != null && motor.MoveInput.sqrMagnitude > 0.01f)
                return motor.MoveInput.normalized;

            if (aim != null && aim.AimDirection.sqrMagnitude > 0.01f)
                return aim.AimDirection;

            return Vector2.down;
        }

        private void CacheComponents()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (aim == null) aim = GetComponent<PlayerAim>();
            if (abilityCaster == null) abilityCaster = GetComponent<AbilityCaster>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (interactor == null) interactor = GetComponent<PlayerInteractor>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        }

        private void CacheAxisActions()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return;
            }

            var map = playerInput.actions.FindActionMap("Player", throwIfNotFound: false);
            if (map == null)
            {
                return;
            }

            _move = map.FindAction("Move", false);
            _aim = map.FindAction("Aim", false);
        }
    }

    public static class PlayerLocateSignal
    {
        public static event System.Action<PlayerInputBridge> Located;

        public static void Raise(PlayerInputBridge player)
        {
            Located?.Invoke(player);
        }
    }
}
