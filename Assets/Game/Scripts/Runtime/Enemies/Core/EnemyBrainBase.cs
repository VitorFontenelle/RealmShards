using UnityEngine;

namespace RealmShards.Enemies
{
    public enum EnemyFsmState
    {
        Idle,
        Chase,
        Telegraph,
        AttackActive,
        Cooldown,
        KeepDistance,
        Aim,
        Shoot,
        Dead
    }

    [RequireComponent(typeof(EnemyMotor))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(EnemySpriteAnimator))]
    public abstract class EnemyBrainBase : MonoBehaviour
    {
        [SerializeField] protected EnemyDefinition definition;

        protected EnemyMotor Motor;
        protected Health Health;
        protected FactionMember Faction;
        protected EnemySpriteAnimator Animator;
        protected EnemyTargetSelector TargetSelector;
        protected EnemyFsmState State = EnemyFsmState.Idle;
        protected float StateEnterTime;
        protected float CooldownUntil;
        protected float ScaledDamage = 8f;
        protected bool RoomActive = true;

        public EnemyDefinition Definition => definition;
        public EnemyFsmState CurrentState => State;

        protected virtual void Awake()
        {
            Motor = GetComponent<EnemyMotor>();
            Health = GetComponent<Health>();
            Faction = GetComponent<FactionMember>();
            Animator = GetComponent<EnemySpriteAnimator>();
            Health.Died += OnDied;
        }

        public virtual void Initialize(EnemyDefinition def, float healthMul, float damageMul)
        {
            definition = def;
            float hp = def != null ? def.MaxHealth * healthMul : 40f * healthMul;
            Health.Configure(hp, 0.05f);
            if (Faction != null)
                Faction.Configure(FactionId.Enemy, 0);

            ScaledDamage = (def != null ? def.AttackDamage : 8f) * damageMul;
            Motor.SetMoveSpeed(def != null ? def.MoveSpeed : 2.4f);

            float retarget = def != null ? def.RetargetInterval : 0.9f;
            float aggro = def != null ? def.AggroRange : 5.5f;
            TargetSelector = new EnemyTargetSelector(transform, retarget, aggro);

            ApplyVisuals();
            Enter(EnemyFsmState.Idle);
        }

        public void SetRoomActive(bool active)
        {
            RoomActive = active;
            Motor.LockMovement(!active);
            if (!active)
                Motor.SetDesiredVelocity(Vector2.zero);
        }

        protected virtual void ApplyVisuals()
        {
            string path = definition != null ? definition.SpritesheetAssetPath : null;
            var all = EnemySpriteLoader.LoadAll(path);
            int walkStart = definition != null ? definition.WalkFrameStart : 0;
            int walkCount = definition != null ? definition.WalkFrameCount : 4;
            int atkStart = definition != null ? definition.AttackFrameStart : 0;
            int atkCount = definition != null ? definition.AttackFrameCount : 4;
            float fps = definition != null ? definition.AnimFps : 8f;
            Color tint = definition != null ? definition.Tint : Color.white;

            var walk = EnemySpriteLoader.Slice(all, walkStart, walkCount);
            var atk = EnemySpriteLoader.Slice(all, atkStart, atkCount);
            var fallback = EnemySpriteLoader.CreatePlaceholder(tint);
            if (walk.Length == 0 && all.Length > 0)
                walk = EnemySpriteLoader.Slice(all, 0, Mathf.Min(4, all.Length));
            if (atk.Length == 0 && all.Length > 4)
                atk = EnemySpriteLoader.Slice(all, Mathf.Min(4, all.Length - 1), Mathf.Min(4, all.Length));

            Animator.Configure(walk, atk, fps, tint, fallback);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sortingLayerName = Core.SortingLayers.Characters;
        }

        protected void Enter(EnemyFsmState next)
        {
            State = next;
            StateEnterTime = Time.time;
            OnEnterState(next);
        }

        protected virtual void OnEnterState(EnemyFsmState state) { }

        protected virtual void Update()
        {
            if (Health == null || !Health.IsAlive)
                return;

            if (!RoomActive)
            {
                Motor.SetDesiredVelocity(Vector2.zero);
                Animator.Tick(Motor.Facing, false);
                return;
            }

            TargetSelector?.Tick(Time.time, PlayerTargetRegistry.Collect());
            TickFsm();
            bool moving = Motor.Velocity.sqrMagnitude > 0.05f;
            Animator.Tick(Motor.Facing, moving);
        }

        protected abstract void TickFsm();

        protected float StateElapsed => Time.time - StateEnterTime;

        private void OnDied(Health _)
        {
            Enter(EnemyFsmState.Dead);
            Motor.LockMovement(true);
            Animator.SetAttacking(false);
            OnEnemyDied();
        }

        protected virtual void OnEnemyDied() { }

        protected virtual void OnDestroy()
        {
            if (Health != null)
                Health.Died -= OnDied;
        }
    }
}
