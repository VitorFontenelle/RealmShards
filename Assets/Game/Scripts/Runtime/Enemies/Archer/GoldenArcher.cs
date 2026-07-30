using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Golden Archer: keep distance, telegraph aim, fire pooled projectile (not painted arrow collider).
    /// </summary>
    public sealed class GoldenArcher : EnemyBrainBase
    {
        [SerializeField] private Transform muzzle;
        private Vector2 _aimDir = Vector2.right;

        protected override void Awake()
        {
            base.Awake();
            if (muzzle == null)
            {
                var go = new GameObject("Muzzle");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0.4f, 0.2f, 0f);
                muzzle = go.transform;
            }
            ProjectilePool.Warm(12);
        }

        protected override void OnEnterState(EnemyFsmState state)
        {
            switch (state)
            {
                case EnemyFsmState.Aim:
                    Motor.LockMovement(true);
                    Animator.SetAttacking(true);
                    break;
                case EnemyFsmState.Shoot:
                    Fire();
                    break;
                case EnemyFsmState.Cooldown:
                    Animator.SetAttacking(false);
                    Motor.LockMovement(false);
                    CooldownUntil = Time.time + (definition != null ? definition.AttackCooldown : 1.4f);
                    break;
                case EnemyFsmState.KeepDistance:
                case EnemyFsmState.Idle:
                    Animator.SetAttacking(false);
                    Motor.LockMovement(false);
                    break;
            }
        }

        protected override void TickFsm()
        {
            var target = TargetSelector?.CurrentTransform;
            float preferred = definition != null ? definition.PreferredDistance : 5.5f;
            float attackRange = definition != null ? definition.AttackRange : 9f;
            float telegraph = definition != null ? definition.TelegraphDuration : 0.55f;

            switch (State)
            {
                case EnemyFsmState.Idle:
                    if (target != null)
                        Enter(EnemyFsmState.KeepDistance);
                    else
                        Motor.SetDesiredVelocity(Vector2.zero);
                    break;

                case EnemyFsmState.KeepDistance:
                    if (target == null)
                    {
                        Enter(EnemyFsmState.Idle);
                        break;
                    }

                    Vector2 to = (Vector2)(target.position - transform.position);
                    float dist = to.magnitude;
                    Motor.Face(to);

                    if (dist < preferred - 0.6f)
                        Motor.SetDesiredVelocity(-to.normalized);
                    else if (dist > preferred + 0.8f)
                        Motor.SetDesiredVelocity(to.normalized);
                    else
                    {
                        // Strafe slightly
                        Vector2 perp = new Vector2(-to.y, to.x).normalized;
                        Motor.SetDesiredVelocity(perp * 0.35f);

                        if (dist <= attackRange && Time.time >= CooldownUntil)
                        {
                            _aimDir = to.normalized;
                            Enter(EnemyFsmState.Aim);
                        }
                    }
                    break;

                case EnemyFsmState.Aim:
                    if (target != null)
                    {
                        _aimDir = ((Vector2)(target.position - transform.position)).normalized;
                        Motor.Face(_aimDir);
                    }
                    Motor.SetDesiredVelocity(Vector2.zero);
                    if (StateElapsed >= telegraph)
                        Enter(EnemyFsmState.Shoot);
                    break;

                case EnemyFsmState.Shoot:
                    Enter(EnemyFsmState.Cooldown);
                    break;

                case EnemyFsmState.Cooldown:
                    Motor.SetDesiredVelocity(Vector2.zero);
                    if (Time.time >= CooldownUntil)
                        Enter(target != null ? EnemyFsmState.KeepDistance : EnemyFsmState.Idle);
                    break;
            }
        }

        private void Fire()
        {
            Vector3 pos = muzzle != null ? muzzle.position : transform.position;
            float speed = definition != null ? definition.ProjectileSpeed : 8f;
            float life = definition != null ? definition.ProjectileLifetime : 3f;
            Color tint = definition != null ? definition.Tint : new Color(1f, 0.85f, 0.2f);
            ProjectilePool.Spawn(pos, _aimDir, speed, life, ScaledDamage, gameObject, Faction, tint);
        }
    }
}
