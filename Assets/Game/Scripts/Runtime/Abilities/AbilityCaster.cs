using System.Collections;
using UnityEngine;

namespace RealmShards
{
    public sealed class AbilityCaster : MonoBehaviour
    {
        public const int SlotCount = 4;

        [SerializeField] private AbilityDefinition basicAbility;
        [SerializeField] private AbilityDefinition ability1;
        [SerializeField] private AbilityDefinition ability2;
        [SerializeField] private AbilityDefinition ability3;
        [SerializeField] private GameObject defaultProjectilePrefab;
        [SerializeField] private GameObject defaultHitboxPrefab;
        [SerializeField] private GameObject defaultOverlayPrefab;
        [SerializeField] private FactionMember factionMember;
        [SerializeField] private Health health;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private DirectionalSpriteAnimator animator;
        [SerializeField] private Color projectileTint = Color.white;

        private readonly float[] _cooldownRemaining = new float[SlotCount];
        private bool _casting;
        private Coroutine _castRoutine;

        public bool IsCasting => _casting;
        public AbilityDefinition BasicAbility => basicAbility;
        public AbilityDefinition Ability1 => ability1;
        public AbilityDefinition Ability2 => ability2;
        public AbilityDefinition Ability3 => ability3;

        private void Awake()
        {
            CacheRefs();
        }

        private void Update()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_cooldownRemaining[i] > 0f)
                {
                    _cooldownRemaining[i] -= Time.deltaTime;
                }
            }
        }

        public void ConfigureDefaults(
            AbilityDefinition basic,
            AbilityDefinition a1,
            AbilityDefinition a2,
            AbilityDefinition a3,
            GameObject projectilePrefab,
            GameObject hitboxPrefab,
            GameObject overlayPrefab)
        {
            basicAbility = basic;
            ability1 = a1;
            ability2 = a2;
            ability3 = a3;
            defaultProjectilePrefab = projectilePrefab;
            defaultHitboxPrefab = hitboxPrefab;
            defaultOverlayPrefab = overlayPrefab;
            CacheRefs();
        }

        public void SetProjectileTint(Color tint)
        {
            projectileTint = tint;
        }

        public void SetAbility(int slot, AbilityDefinition definition)
        {
            switch (Mathf.Clamp(slot, 0, 3))
            {
                case 0: basicAbility = definition; break;
                case 1: ability1 = definition; break;
                case 2: ability2 = definition; break;
                case 3: ability3 = definition; break;
            }
        }

        public AbilityDefinition GetAbility(int slot)
        {
            return slot switch
            {
                0 => basicAbility,
                1 => ability1,
                2 => ability2,
                3 => ability3,
                _ => null
            };
        }

        public float GetCooldownRemaining(int slot)
        {
            slot = Mathf.Clamp(slot, 0, SlotCount - 1);
            return Mathf.Max(0f, _cooldownRemaining[slot]);
        }

        public bool TryCast(int slot, Vector2 aimDirection)
        {
            if (_casting || !isActiveAndEnabled)
            {
                return false;
            }

            var ability = GetAbility(slot);
            if (ability == null)
            {
                return false;
            }

            if (_cooldownRemaining[slot] > 0f)
            {
                return false;
            }

            if (aimDirection.sqrMagnitude < 0.001f)
            {
                aimDirection = Vector2.right;
            }

            aimDirection.Normalize();
            _castRoutine = StartCoroutine(CastRoutine(slot, ability, aimDirection));
            return true;
        }

        public void CancelCast()
        {
            if (_castRoutine != null)
            {
                StopCoroutine(_castRoutine);
                _castRoutine = null;
            }

            _casting = false;
            motor?.SetCastLocked(false);
        }

        private IEnumerator CastRoutine(int slot, AbilityDefinition ability, Vector2 aim)
        {
            _casting = true;
            animator?.PlayCast();
            motor?.SetCastLocked(true);

            SpawnOverlay(ability, aim);

            if (ability.Windup > 0f)
            {
                yield return new WaitForSeconds(ability.Windup);
            }

            ExecuteEffect(ability, aim);

            float lockTime = Mathf.Max(ability.CastLockMovement, ability.ActiveDuration + ability.Recovery);
            float unlockAt = Time.time + lockTime;

            if (ability.ActiveDuration > 0f)
            {
                yield return new WaitForSeconds(ability.ActiveDuration);
            }

            if (ability.Recovery > 0f)
            {
                yield return new WaitForSeconds(ability.Recovery);
            }

            _cooldownRemaining[slot] = ability.Cooldown;
            _casting = false;
            _castRoutine = null;

            float remainingLock = unlockAt - Time.time;
            if (remainingLock > 0f)
            {
                yield return new WaitForSeconds(remainingLock);
            }

            motor?.SetCastLocked(false);
        }

        private void ExecuteEffect(AbilityDefinition ability, Vector2 aim)
        {
            var ctx = BuildContext(aim);
            switch (ability.Kind)
            {
                case AbilityKind.Projectile:
                    FireProjectile(ability, ctx);
                    break;
                case AbilityKind.MeleeHitbox:
                    SpawnMelee(ability, ctx);
                    break;
                case AbilityKind.Dash:
                    StartCoroutine(DashRoutine(ability, ctx));
                    break;
            }
        }

        private void FireProjectile(AbilityDefinition ability, AbilityContext ctx)
        {
            var prefab = ability.ProjectilePrefab != null ? ability.ProjectilePrefab : defaultProjectilePrefab;
            if (prefab == null)
            {
                return;
            }

            var projectile = PoolHub.Instance.Spawn<Projectile>(
                prefab,
                ctx.Origin + ctx.AimDirection * 0.4f,
                Quaternion.identity);

            if (projectile == null)
            {
                var go = Instantiate(prefab, ctx.Origin + ctx.AimDirection * 0.4f, Quaternion.identity);
                projectile = go.GetComponent<Projectile>();
            }

            projectile?.Launch(
                ctx.Origin + ctx.AimDirection * 0.4f,
                ctx.AimDirection,
                ctx.Faction,
                ability.Damage,
                ability.Knockback,
                ability.ProjectileSpeed,
                ability.Range / Mathf.Max(0.1f, ability.ProjectileSpeed),
                ability.Pierce,
                projectileTint);
        }

        private void SpawnMelee(AbilityDefinition ability, AbilityContext ctx)
        {
            var prefab = ability.HitboxPrefab != null ? ability.HitboxPrefab : defaultHitboxPrefab;
            if (prefab == null)
            {
                return;
            }

            Vector2 pos = ctx.Origin + ctx.AimDirection * ability.HitboxDistance;
            var hitbox = PoolHub.Instance.Spawn<Hitbox>(prefab, pos, Quaternion.identity);
            if (hitbox == null)
            {
                var go = Instantiate(prefab, pos, Quaternion.identity);
                hitbox = go.GetComponent<Hitbox>();
            }

            hitbox?.Activate(
                pos,
                ctx.AimDirection,
                ctx.Faction,
                ability.Damage,
                ability.Knockback,
                ability.ActiveDuration > 0f ? ability.ActiveDuration : 0.12f,
                ctx.CasterTransform,
                ctx.AimDirection * ability.HitboxDistance,
                ability.Pierce);

            if (hitbox != null)
            {
                hitbox.transform.localScale = Vector3.one * Mathf.Max(0.25f, ability.HitboxRadius);
            }
        }

        private IEnumerator DashRoutine(AbilityDefinition ability, AbilityContext ctx)
        {
            if (ctx.Motor == null && ctx.CasterBody == null)
            {
                yield break;
            }

            if (ability.DashInvulnerable && ctx.Health != null)
            {
                ctx.Health.PulseIFrames(ability.DashDuration + 0.05f);
            }

            Vector2 start = ctx.Origin;
            Vector2 end = start + ctx.AimDirection * ability.DashDistance;
            float duration = Mathf.Max(0.01f, ability.DashDuration);
            float t = 0f;

            ctx.Motor?.SetDashing(true);

            while (t < duration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                Vector2 p = Vector2.Lerp(start, end, a);
                if (ctx.CasterBody != null)
                {
                    ctx.CasterBody.MovePosition(p);
                    ctx.CasterBody.linearVelocity = Vector2.zero;
                }
                else if (ctx.CasterTransform != null)
                {
                    ctx.CasterTransform.position = p;
                }

                yield return null;
            }

            ctx.Motor?.SetDashing(false);
        }

        private void SpawnOverlay(AbilityDefinition ability, Vector2 aim)
        {
            var prefab = ability.EffectOverlayPrefab != null ? ability.EffectOverlayPrefab : defaultOverlayPrefab;
            if (prefab == null)
            {
                return;
            }

            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            var go = PoolHub.Instance.Spawn(prefab, origin, Quaternion.identity);
            if (go == null)
            {
                go = Instantiate(prefab, origin, Quaternion.identity);
            }

            var overlay = go.GetComponent<AbilityEffectOverlay>();
            overlay?.Play(transform, aim, ability.TotalCastTime, projectileTint);
        }

        private AbilityContext BuildContext(Vector2 aim)
        {
            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            return new AbilityContext
            {
                CasterTransform = transform,
                CasterBody = body,
                Faction = factionMember,
                Health = health,
                Origin = origin,
                AimDirection = aim,
                Caster = this,
                Motor = motor
            };
        }

        private void CacheRefs()
        {
            if (factionMember == null) factionMember = GetComponent<FactionMember>();
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (animator == null) animator = GetComponentInChildren<DirectionalSpriteAnimator>();
        }
    }
}
