using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Magic
{
    /// <summary>
    /// Reusable status effects driven from Health (Burn DoT, Slow, Ward shield).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class StatusEffectHost : MonoBehaviour
    {
        private sealed class ActiveStatus
        {
            public StatusEffectType Type;
            public float Remaining;
            public float Magnitude;
            public float TickInterval;
            public float TickTimer;
            public float WardRemaining;
        }

        [SerializeField] private Health health;
        [SerializeField] private PlayerMotor motor;

        private readonly List<ActiveStatus> _active = new List<ActiveStatus>(8);
        private float _baseSpeedBonusCaptured;
        private bool _slowApplied;
        private float _slowAmount;

        public float WardAbsorbRemaining
        {
            get
            {
                float w = 0f;
                for (int i = 0; i < _active.Count; i++)
                    if (_active[i].Type == StatusEffectType.Ward)
                        w += _active[i].WardRemaining;
                return w;
            }
        }

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
        }

        private void OnEnable()
        {
            if (health != null)
                health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Damaged -= OnDamaged;
            ClearSlow();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var s = _active[i];
                s.Remaining -= dt;
                if (s.Type == StatusEffectType.Burn && s.TickInterval > 0f && health != null && health.IsAlive)
                {
                    s.TickTimer -= dt;
                    if (s.TickTimer <= 0f)
                    {
                        s.TickTimer = s.TickInterval;
                        health.TakeDamage(s.Magnitude, gameObject);
                    }
                }

                if (s.Remaining <= 0f)
                {
                    if (s.Type == StatusEffectType.Slow)
                        ClearSlow();
                    _active.RemoveAt(i);
                }
            }
        }

        public void Apply(StatusApplication app)
        {
            if (app.type == StatusEffectType.None || app.duration <= 0f)
                return;

            if (app.type == StatusEffectType.KnockbackWave)
            {
                ApplyKnockbackWave(app.magnitude);
                return;
            }

            var existing = Find(app.type);
            if (existing != null)
            {
                existing.Remaining = Mathf.Max(existing.Remaining, app.duration);
                existing.Magnitude = Mathf.Max(existing.Magnitude, app.magnitude);
                if (app.type == StatusEffectType.Ward)
                    existing.WardRemaining = Mathf.Max(existing.WardRemaining, app.magnitude);
                return;
            }

            var s = new ActiveStatus
            {
                Type = app.type,
                Remaining = app.duration,
                Magnitude = app.magnitude,
                TickInterval = app.tickInterval > 0f ? app.tickInterval : 0.5f,
                TickTimer = app.tickInterval > 0f ? app.tickInterval : 0.5f,
                WardRemaining = app.type == StatusEffectType.Ward ? app.magnitude : 0f
            };
            _active.Add(s);

            if (app.type == StatusEffectType.Slow)
                ApplySlow(app.magnitude);
        }

        public void ApplyFromAbility(AbilityDefinition ability)
        {
            if (ability == null) return;
            var apps = ability.StatusEffects;
            if (apps == null) return;
            for (int i = 0; i < apps.Length; i++)
                Apply(apps[i]);
        }

        private void OnDamaged(Health h, DamageInfo info)
        {
            // Ward absorption is handled via AbsorbDamage if wired; soft note for now.
        }

        /// <summary>Consumes ward before HP. Returns remaining damage after absorb.</summary>
        public float AbsorbDamage(float amount)
        {
            float left = amount;
            for (int i = 0; i < _active.Count && left > 0f; i++)
            {
                if (_active[i].Type != StatusEffectType.Ward || _active[i].WardRemaining <= 0f)
                    continue;
                float take = Mathf.Min(_active[i].WardRemaining, left);
                _active[i].WardRemaining -= take;
                left -= take;
            }

            return left;
        }

        private void ApplySlow(float amount)
        {
            if (motor == null) return;
            if (_slowApplied)
                motor.AddMoveSpeedBonus(_slowAmount);
            _slowAmount = Mathf.Clamp(amount, 0.1f, 3f);
            motor.AddMoveSpeedBonus(-_slowAmount);
            _slowApplied = true;
        }

        private void ClearSlow()
        {
            if (!_slowApplied || motor == null) return;
            motor.AddMoveSpeedBonus(_slowAmount);
            _slowApplied = false;
            _slowAmount = 0f;
        }

        private void ApplyKnockbackWave(float force)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;
            // Radial push away from nearest enemy caster omitted — push outward from self origin noise.
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }

        private ActiveStatus Find(StatusEffectType type)
        {
            for (int i = 0; i < _active.Count; i++)
                if (_active[i].Type == type)
                    return _active[i];
            return null;
        }
    }
}
