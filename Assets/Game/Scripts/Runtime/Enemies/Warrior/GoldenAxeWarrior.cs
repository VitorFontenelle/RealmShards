using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Golden Axe Warrior: approach → telegraph → active hitbox window → cooldown.
    /// </summary>
    public class GoldenAxeWarrior : EnemyBrainBase
    {
        [SerializeField] private Transform hitboxAnchor;
        private EnemyHitbox _hitbox;
        private Vector2 _attackDir = Vector2.right;

        protected override void Awake()
        {
            base.Awake();
            EnsureHitbox();
        }

        public override void Initialize(EnemyDefinition def, float healthMul, float damageMul)
        {
            base.Initialize(def, healthMul, damageMul);
            EnsureHitbox();
            float radius = def != null ? def.HitboxRadius : 0.85f;
            _hitbox.Configure(ScaledDamage, radius, GetComponent<FactionMember>());
            _hitbox.SetActiveWindow(false);
        }

        private void EnsureHitbox()
        {
            if (hitboxAnchor == null)
            {
                var go = new GameObject("MeleeHitbox");
                go.transform.SetParent(transform);
                go.transform.localPosition = Vector3.zero;
                hitboxAnchor = go.transform;
                go.AddComponent<CircleCollider2D>();
                _hitbox = go.AddComponent<EnemyHitbox>();
            }
            else
            {
                _hitbox = hitboxAnchor.GetComponent<EnemyHitbox>();
                if (_hitbox == null)
                    _hitbox = hitboxAnchor.gameObject.AddComponent<EnemyHitbox>();
                if (hitboxAnchor.GetComponent<CircleCollider2D>() == null)
                    hitboxAnchor.gameObject.AddComponent<CircleCollider2D>();
            }
        }

        protected override void OnEnterState(EnemyFsmState state)
        {
            switch (state)
            {
                case EnemyFsmState.Telegraph:
                    Motor.LockMovement(true);
                    Animator.SetAttacking(true);
                    _hitbox.SetActiveWindow(false);
                    break;
                case EnemyFsmState.AttackActive:
                    _hitbox.SetActiveWindow(true);
                    PositionHitbox();
                    break;
                case EnemyFsmState.Cooldown:
                    _hitbox.SetActiveWindow(false);
                    Animator.SetAttacking(false);
                    Motor.LockMovement(false);
                    CooldownUntil = Time.time + (definition != null ? definition.AttackCooldown : 1.1f);
                    break;
                case EnemyFsmState.Chase:
                case EnemyFsmState.Idle:
                    _hitbox.SetActiveWindow(false);
                    Animator.SetAttacking(false);
                    Motor.LockMovement(false);
                    break;
                case EnemyFsmState.Dead:
                    _hitbox.SetActiveWindow(false);
                    break;
            }
        }

        protected override void TickFsm()
        {
            var target = TargetSelector?.CurrentTransform;
            float attackRange = definition != null ? definition.AttackRange : 1.35f;
            float telegraph = definition != null ? definition.TelegraphDuration : 0.45f;
            float active = definition != null ? definition.ActiveHitDuration : 0.18f;
            float offset = definition != null ? definition.HitboxForwardOffset : 0.7f;

            switch (State)
            {
                case EnemyFsmState.Idle:
                    if (target != null)
                        Enter(EnemyFsmState.Chase);
                    else
                        Motor.SetDesiredVelocity(Vector2.zero);
                    break;

                case EnemyFsmState.Chase:
                    if (target == null)
                    {
                        Enter(EnemyFsmState.Idle);
                        break;
                    }

                    Vector2 to = (Vector2)(target.position - transform.position);
                    float dist = to.magnitude;
                    Motor.Face(to);
                    if (dist <= attackRange)
                    {
                        _attackDir = to.sqrMagnitude > 0.001f ? to.normalized : Motor.Facing;
                        Enter(EnemyFsmState.Telegraph);
                    }
                    else
                    {
                        Motor.SetDesiredVelocity(to.normalized);
                    }
                    break;

                case EnemyFsmState.Telegraph:
                    if (target != null)
                    {
                        _attackDir = ((Vector2)(target.position - transform.position)).normalized;
                        Motor.Face(_attackDir);
                    }
                    PositionHitbox(offset);
                    if (StateElapsed >= telegraph)
                        Enter(EnemyFsmState.AttackActive);
                    break;

                case EnemyFsmState.AttackActive:
                    PositionHitbox(offset);
                    if (StateElapsed >= active)
                        Enter(EnemyFsmState.Cooldown);
                    break;

                case EnemyFsmState.Cooldown:
                    Motor.SetDesiredVelocity(Vector2.zero);
                    if (Time.time >= CooldownUntil)
                        Enter(target != null ? EnemyFsmState.Chase : EnemyFsmState.Idle);
                    break;
            }
        }

        private void PositionHitbox(float? offsetOverride = null)
        {
            if (hitboxAnchor == null)
                return;
            float offset = offsetOverride ?? (definition != null ? definition.HitboxForwardOffset : 0.7f);
            hitboxAnchor.localPosition = (Vector3)(_attackDir * offset);
        }
    }
}
