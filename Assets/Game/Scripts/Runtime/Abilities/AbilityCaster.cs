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
        [SerializeField] private PlayerItemModifiers modifiers;
        [SerializeField] private PlayerInventory inventory;
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
                    _cooldownRemaining[i] -= Time.deltaTime;
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

        public void SetProjectileTint(Color tint) => projectileTint = tint;

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

        public AbilityDefinition GetAbility(int slot) => slot switch
        {
            0 => basicAbility,
            1 => ability1,
            2 => ability2,
            3 => ability3,
            _ => null
        };

        public float GetCooldownRemaining(int slot)
        {
            slot = Mathf.Clamp(slot, 0, SlotCount - 1);
            return Mathf.Max(0f, _cooldownRemaining[slot]);
        }

        public float GetCooldownNormalized(int slot)
        {
            var ability = GetAbility(slot);
            if (ability == null) return 0f;
            float cd = EffectiveCooldown(ability);
            if (cd <= 0.01f) return 0f;
            return Mathf.Clamp01(GetCooldownRemaining(slot) / cd);
        }

        public bool TryCast(int slot, Vector2 aimDirection)
        {
            if (_casting || !isActiveAndEnabled)
                return false;

            var ability = GetAbility(slot);
            if (ability == null || _cooldownRemaining[slot] > 0f)
                return false;

            if (aimDirection.sqrMagnitude < 0.001f)
                aimDirection = Vector2.right;

            aimDirection.Normalize();
            animator?.SetFacingFromVector(aimDirection);
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
            _currentCastSlot = slot;
            _casting = true;
            animator?.PlayCast();
            motor?.SetCastLocked(true);
            SpawnOverlay(ability, aim);

            if (ability.Windup > 0f)
                yield return new WaitForSeconds(ability.Windup);

            ExecuteEffect(ability, aim);

            float lockTime = Mathf.Max(ability.CastLockMovement, ability.ActiveDuration + ability.Recovery);
            float unlockAt = Time.time + lockTime;

            if (ability.ActiveDuration > 0f)
                yield return new WaitForSeconds(ability.ActiveDuration);
            if (ability.Recovery > 0f)
                yield return new WaitForSeconds(ability.Recovery);

            _cooldownRemaining[slot] = EffectiveCooldown(ability);
            _casting = false;
            _castRoutine = null;

            float remainingLock = unlockAt - Time.time;
            if (remainingLock > 0f)
                yield return new WaitForSeconds(remainingLock);

            motor?.SetCastLocked(false);
        }

        private float EffectiveCooldown(AbilityDefinition ability)
        {
            float cd = ability.Cooldown;
            return modifiers != null ? modifiers.ScaleCooldown(cd) : cd;
        }

        private float EffectiveDamage(AbilityDefinition ability, int slot)
        {
            float dmg = ability.Damage;
            if (modifiers != null)
                dmg = modifiers.ScaleDamage(dmg);
            var runtime = GetComponent<PlayerLoadoutRuntime>();
            if (runtime != null)
                dmg *= runtime.GetDamageMultiplier(slot);
            return dmg;
        }

        private AbilityDefinition _lastCastAbility;
        private int _currentCastSlot;

        private void ExecuteEffect(AbilityDefinition ability, Vector2 aim)
        {
            _lastCastAbility = ability;
            var ctx = BuildContext(aim);
            Audio.AudioEventHub.Play("ability.cast", transform.position);

            if (ability.ApplyStatusesToSelf)
                ApplyStatusesToSelf(ability);

            switch (ability.Kind)
            {
                case AbilityKind.Projectile:
                    FireProjectile(ability, ctx, aim);
                    break;
                case AbilityKind.MeleeHitbox:
                    SpawnMelee(ability, ctx);
                    ApplyStatusesInRadius(ability, ctx);
                    break;
                case AbilityKind.Dash:
                    StartCoroutine(DashRoutine(ability, ctx));
                    break;
            }
        }

        private void ApplyStatusesToSelf(AbilityDefinition ability)
        {
            if (health == null || ability?.StatusEffects == null) return;
            var host = health.GetComponent<Magic.StatusEffectHost>();
            if (host == null)
                host = health.gameObject.AddComponent<Magic.StatusEffectHost>();
            for (int i = 0; i < ability.StatusEffects.Length; i++)
                host.Apply(ability.StatusEffects[i]);
        }

        private void ApplyStatusesInRadius(AbilityDefinition ability, AbilityContext ctx)
        {
            if (ability.StatusEffects == null || ability.StatusEffects.Length == 0)
                return;
            float radius = ability.HitboxRadius;
            if (modifiers != null)
                radius = modifiers.ScalePulseRadius(radius);
            Vector2 pos = ctx.Origin + ctx.AimDirection * ability.HitboxDistance;
            var hits = Physics2D.OverlapCircleAll(pos, radius);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i].GetComponentInParent<Health>();
                if (h == null || h == health) continue;
                ApplyStatusesToVictim(h, ability);
            }
        }

        private static void ApplyStatusesToVictim(Health victim, AbilityDefinition ability)
        {
            if (victim == null || ability == null || ability.StatusEffects == null)
                return;
            if (ability.ApplyStatusesToSelf)
                return; // Continuum Echo ward etc. — self only
            var host = victim.GetComponent<Magic.StatusEffectHost>();
            if (host == null)
                host = victim.gameObject.AddComponent<Magic.StatusEffectHost>();
            for (int i = 0; i < ability.StatusEffects.Length; i++)
                host.Apply(ability.StatusEffects[i]);
        }

        private void FireProjectile(AbilityDefinition ability, AbilityContext ctx, Vector2 aim)
        {
            bool pierce = ability.Pierce || (modifiers != null && modifiers.BoltPierce);
            int extras = modifiers != null ? modifiers.BoltSplitExtra : 0;
            int total = 1 + extras;
            float spread = extras > 0 ? 18f : 0f;

            for (int i = 0; i < total; i++)
            {
                float angleOffset = 0f;
                if (total > 1)
                    angleOffset = Mathf.Lerp(-spread, spread, total == 1 ? 0.5f : i / (float)(total - 1));

                Vector2 dir = Rotate(aim, angleOffset);
                SpawnOneProjectile(ability, ctx, dir, pierce);
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad);
            float s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c).normalized;
        }

        private void SpawnOneProjectile(AbilityDefinition ability, AbilityContext ctx, Vector2 dir, bool pierce)
        {
            var prefab = ability.ProjectilePrefab != null ? ability.ProjectilePrefab : defaultProjectilePrefab;
            if (prefab == null) return;

            Vector2 spawn = ctx.Origin + dir * 0.4f;
            var projectile = PoolHub.Instance != null
                ? PoolHub.Instance.Spawn<Projectile>(prefab, spawn, Quaternion.identity)
                : null;
            if (projectile == null)
            {
                var go = Instantiate(prefab, spawn, Quaternion.identity);
                projectile = go.GetComponent<Projectile>();
            }

            projectile?.Launch(
                spawn,
                dir,
                ctx.Faction,
                EffectiveDamage(ability, _currentCastSlot),
                ability.Knockback,
                ability.ProjectileSpeed,
                12f,
                pierce,
                projectileTint,
                OnProjectileHit);
        }

        private void OnProjectileHit(DamageInfo info, Health victim)
        {
            inventory?.NotifyPlayerDealtDamage(in info, victim);
            ApplyStatusesToVictim(victim, _lastCastAbility);
        }

        private void SpawnMelee(AbilityDefinition ability, AbilityContext ctx)
        {
            var prefab = ability.HitboxPrefab != null ? ability.HitboxPrefab : defaultHitboxPrefab;
            if (prefab == null) return;

            Vector2 pos = ctx.Origin + ctx.AimDirection * ability.HitboxDistance;
            var hitbox = PoolHub.Instance != null
                ? PoolHub.Instance.Spawn<Hitbox>(prefab, pos, Quaternion.identity)
                : null;
            if (hitbox == null)
            {
                var go = Instantiate(prefab, pos, Quaternion.identity);
                hitbox = go.GetComponent<Hitbox>();
            }

            float radius = ability.HitboxRadius;
            if (modifiers != null)
                radius = modifiers.ScalePulseRadius(radius);

            hitbox?.Activate(
                pos,
                ctx.AimDirection,
                ctx.Faction,
                EffectiveDamage(ability, _currentCastSlot),
                ability.Knockback,
                ability.ActiveDuration > 0f ? ability.ActiveDuration : 0.12f,
                ctx.CasterTransform,
                ctx.AimDirection * ability.HitboxDistance,
                ability.Pierce);

            if (hitbox != null)
                hitbox.transform.localScale = Vector3.one * Mathf.Max(0.25f, radius);
        }

        private IEnumerator DashRoutine(AbilityDefinition ability, AbilityContext ctx)
        {
            if (ctx.Motor == null && ctx.CasterBody == null)
                yield break;

            if (ability.DashInvulnerable && ctx.Health != null)
                ctx.Health.PulseIFrames(ability.DashDuration + 0.05f);

            float distance = ability.DashDistance;
            if (modifiers != null)
                distance = modifiers.ScaleBlinkDistance(distance);

            Vector2 start = ctx.Origin;
            Vector2 end = start + ctx.AimDirection * distance;
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
            if (prefab == null) return;

            Vector2 origin = body != null ? body.position : (Vector2)transform.position;
            var go = PoolHub.Instance != null
                ? PoolHub.Instance.Spawn(prefab, origin, Quaternion.identity)
                : null;
            if (go == null)
                go = Instantiate(prefab, origin, Quaternion.identity);

            go.GetComponent<AbilityEffectOverlay>()?.Play(transform, aim, ability.TotalCastTime, projectileTint);
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
            if (modifiers == null) modifiers = GetComponent<PlayerItemModifiers>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
        }
    }
}
