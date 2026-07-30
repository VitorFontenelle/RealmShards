using System;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Multi-phase Arcane Core boss: telegraphed cleave, slam, and phase-2 burst.
    /// Clear telegraph windows; damage only during AttackActive.
    /// </summary>
    public sealed class ArcaneCoreChampion : EnemyBrainBase
    {
        private enum AttackKind
        {
            Cleave = 0,
            Slam = 1,
            Burst = 2
        }

        [SerializeField] private bool spawnArcaneCoreOnDeath = true;
        [SerializeField] private GameObject arcaneCorePrefab;
        [SerializeField] private Transform hitboxAnchor;
        [SerializeField] private float phase2HealthFraction = 0.5f;
        [SerializeField] private float slamRadius = 2.4f;
        [SerializeField] private float burstRadius = 3.2f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.55f, 0.2f, 1f);
        [SerializeField] private Color phase2Tint = new Color(1f, 0.35f, 0.55f, 1f);

        private EnemyHitbox _hitbox;
        private SpriteRenderer _sr;
        private Color _baseTint = Color.white;
        private Vector2 _attackDir = Vector2.right;
        private AttackKind _nextAttack = AttackKind.Cleave;
        private bool _phase2;
        private GameObject _telegraphRing;
        private SpriteRenderer _telegraphSr;

        public bool IsPhase2 => _phase2;
        public event Action<ArcaneCoreChampion> ChampionDefeated;
        public event Action<ArcaneCoreChampion> PhaseChanged;

        protected override void Awake()
        {
            base.Awake();
            _sr = GetComponent<SpriteRenderer>();
            EnsureHitbox();
            EnsureTelegraphRing();
        }

        public override void Initialize(EnemyDefinition def, float healthMul, float damageMul)
        {
            base.Initialize(def, healthMul, damageMul);
            EnsureHitbox();
            EnsureTelegraphRing();
            float radius = def != null ? def.HitboxRadius : 1.1f;
            _hitbox.Configure(ScaledDamage, radius, GetComponent<FactionMember>());
            _hitbox.SetActiveWindow(false);
            _baseTint = def != null ? def.Tint : new Color(0.75f, 0.45f, 1f);
            if (_sr != null) _sr.color = _baseTint;
            _phase2 = false;
            if (Health != null)
                Health.Damaged += OnDamaged;
        }

        private void OnDamaged(Health _, DamageInfo __) => TryEnterPhase2();

        private void TryEnterPhase2()
        {
            if (_phase2 || Health == null || Health.MaxHealth <= 0f)
                return;
            if (Health.CurrentHealth / Health.MaxHealth > phase2HealthFraction)
                return;

            _phase2 = true;
            if (_sr != null)
                _sr.color = phase2Tint;
            _baseTint = phase2Tint;
            Motor.SetMoveSpeed((definition != null ? definition.MoveSpeed : 2f) * 1.25f);
            ScaledDamage *= 1.15f;
            PhaseChanged?.Invoke(this);
            Debug.Log("[ArcaneCoreChampion] Phase 2");
        }

        private void EnsureHitbox()
        {
            if (hitboxAnchor == null)
            {
                var go = new GameObject("ChampionHitbox");
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

        private void EnsureTelegraphRing()
        {
            if (_telegraphRing != null) return;
            _telegraphRing = new GameObject("TelegraphRing");
            _telegraphRing.transform.SetParent(transform);
            _telegraphRing.transform.localPosition = Vector3.zero;
            _telegraphSr = _telegraphRing.AddComponent<SpriteRenderer>();
            _telegraphSr.sprite = EnemySpriteLoader.CreatePlaceholder(telegraphColor, 64);
            _telegraphSr.sortingLayerName = Core.SortingLayers.SkillEffectsFront;
            _telegraphSr.sortingOrder = 5;
            _telegraphSr.color = new Color(telegraphColor.r, telegraphColor.g, telegraphColor.b, 0.35f);
            _telegraphRing.SetActive(false);
        }

        protected override void OnEnterState(EnemyFsmState state)
        {
            switch (state)
            {
                case EnemyFsmState.Telegraph:
                    Motor.LockMovement(true);
                    Animator.SetAttacking(true);
                    _hitbox.SetActiveWindow(false);
                    ShowTelegraph(true);
                    if (_sr != null)
                        _sr.color = Color.Lerp(_baseTint, telegraphColor, 0.55f);
                    break;
                case EnemyFsmState.AttackActive:
                    ShowTelegraph(false);
                    _hitbox.SetActiveWindow(true);
                    PositionHitboxForAttack();
                    if (_sr != null)
                        _sr.color = Color.white;
                    Combat.HitStop.Request(_phase2 ? 0.07f : 0.04f);
                    break;
                case EnemyFsmState.Cooldown:
                    ShowTelegraph(false);
                    _hitbox.SetActiveWindow(false);
                    Animator.SetAttacking(false);
                    Motor.LockMovement(false);
                    if (_sr != null) _sr.color = _baseTint;
                    float cd = definition != null ? definition.AttackCooldown : 1f;
                    if (_phase2) cd *= 0.7f;
                    CooldownUntil = Time.time + cd;
                    PickNextAttack();
                    break;
                case EnemyFsmState.Chase:
                case EnemyFsmState.Idle:
                    ShowTelegraph(false);
                    _hitbox.SetActiveWindow(false);
                    Animator.SetAttacking(false);
                    Motor.LockMovement(false);
                    if (_sr != null) _sr.color = _baseTint;
                    break;
                case EnemyFsmState.Dead:
                    ShowTelegraph(false);
                    _hitbox.SetActiveWindow(false);
                    break;
            }
        }

        protected override void TickFsm()
        {
            var target = TargetSelector?.CurrentTransform;
            float attackRange = definition != null ? definition.AttackRange : 1.5f;
            float telegraph = definition != null ? definition.TelegraphDuration : 0.55f;
            float active = definition != null ? definition.ActiveHitDuration : 0.22f;
            if (_phase2)
            {
                telegraph *= 0.75f;
                active *= 1.1f;
            }

            // Slam / burst need longer telegraph for readability.
            if (_nextAttack == AttackKind.Slam) telegraph *= 1.25f;
            if (_nextAttack == AttackKind.Burst) telegraph *= 1.35f;

            switch (State)
            {
                case EnemyFsmState.Idle:
                    if (target != null) Enter(EnemyFsmState.Chase);
                    else Motor.SetDesiredVelocity(Vector2.zero);
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
                    float engage = _nextAttack == AttackKind.Burst ? attackRange + 1.2f
                        : _nextAttack == AttackKind.Slam ? attackRange + 0.6f
                        : attackRange;

                    if (dist <= engage)
                    {
                        _attackDir = to.sqrMagnitude > 0.001f ? to.normalized : Motor.Facing;
                        Enter(EnemyFsmState.Telegraph);
                    }
                    else
                    {
                        Motor.SetDesiredVelocity(to.normalized * (_phase2 ? 1.15f : 1f));
                    }
                    break;

                case EnemyFsmState.Telegraph:
                    if (target != null)
                    {
                        _attackDir = ((Vector2)(target.position - transform.position)).normalized;
                        Motor.Face(_attackDir);
                    }
                    PositionHitboxForAttack();
                    PulseTelegraph();
                    if (StateElapsed >= telegraph)
                        Enter(EnemyFsmState.AttackActive);
                    break;

                case EnemyFsmState.AttackActive:
                    PositionHitboxForAttack();
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

        private void PickNextAttack()
        {
            if (!_phase2)
            {
                _nextAttack = _nextAttack == AttackKind.Cleave ? AttackKind.Slam : AttackKind.Cleave;
                return;
            }

            // Phase 2: rotate cleave → slam → burst.
            _nextAttack = _nextAttack switch
            {
                AttackKind.Cleave => AttackKind.Slam,
                AttackKind.Slam => AttackKind.Burst,
                _ => AttackKind.Cleave
            };
        }

        private void PositionHitboxForAttack()
        {
            if (hitboxAnchor == null || _hitbox == null) return;
            float offset = definition != null ? definition.HitboxForwardOffset : 0.7f;
            float radius = definition != null ? definition.HitboxRadius : 1.1f;

            switch (_nextAttack)
            {
                case AttackKind.Slam:
                    hitboxAnchor.localPosition = Vector3.zero;
                    _hitbox.Configure(ScaledDamage * 1.25f, slamRadius, GetComponent<FactionMember>());
                    break;
                case AttackKind.Burst:
                    hitboxAnchor.localPosition = Vector3.zero;
                    _hitbox.Configure(ScaledDamage * 0.85f, burstRadius, GetComponent<FactionMember>());
                    break;
                default:
                    hitboxAnchor.localPosition = (Vector3)(_attackDir * offset);
                    _hitbox.Configure(ScaledDamage, radius, GetComponent<FactionMember>());
                    break;
            }
        }

        private void ShowTelegraph(bool on)
        {
            if (_telegraphRing == null) return;
            _telegraphRing.SetActive(on);
            if (!on) return;
            float scale = _nextAttack switch
            {
                AttackKind.Slam => slamRadius * 2.2f,
                AttackKind.Burst => burstRadius * 2.2f,
                _ => 1.6f
            };
            _telegraphRing.transform.localScale = Vector3.one * scale;
            if (_nextAttack == AttackKind.Cleave)
                _telegraphRing.transform.localPosition = (Vector3)(_attackDir * 0.7f);
            else
                _telegraphRing.transform.localPosition = Vector3.zero;
        }

        private void PulseTelegraph()
        {
            if (_telegraphSr == null || !_telegraphRing.activeSelf) return;
            float pulse = 0.25f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 10f));
            var c = telegraphColor;
            c.a = pulse;
            _telegraphSr.color = c;
        }

        protected override void OnEnemyDied()
        {
            base.OnEnemyDied();
            ChampionDefeated?.Invoke(this);
            Audio.AudioEventHub.Play("champion.death", transform.position);

            if (!spawnArcaneCoreOnDeath)
                return;

            if (arcaneCorePrefab != null)
                Instantiate(arcaneCorePrefab, transform.position, Quaternion.identity);
            else
                ArcaneCoreTrigger.SpawnStub(transform.position);
        }

        protected override void OnDestroy()
        {
            if (Health != null)
                Health.Damaged -= OnDamaged;
            base.OnDestroy();
        }
    }
}
