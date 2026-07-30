using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Enemies;
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
        [SerializeField] private SpriteRenderer tintTarget;
        [SerializeField] private bool showStatusTint = true;

        private readonly List<ActiveStatus> _active = new List<ActiveStatus>(8);
        private bool _slowApplied;
        private float _slowAmount;
        private Color _baseColor = Color.white;
        private bool _capturedBase;
        private GameObject _wardRing;
        private SpriteRenderer _wardSr;

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

        public bool HasBurn => Find(StatusEffectType.Burn) != null;
        public bool HasSlow => Find(StatusEffectType.Slow) != null;
        public bool HasWard => WardAbsorbRemaining > 0.1f;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (tintTarget == null) tintTarget = GetComponent<SpriteRenderer>();
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
            RestoreTint();
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

                if (s.Type == StatusEffectType.Ward && s.WardRemaining <= 0.01f)
                    s.Remaining = 0f;

                if (s.Remaining <= 0f)
                {
                    if (s.Type == StatusEffectType.Slow)
                        ClearSlow();
                    _active.RemoveAt(i);
                }
            }

            RefreshVisuals();
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
                if (app.type == StatusEffectType.Slow)
                    ApplySlow(existing.Magnitude);
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
            // Absorb already applied in Health before Damaged fires.
            RefreshVisuals();
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
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;
            rb.AddForce(dir * force, ForceMode2D.Impulse);
        }

        private void RefreshVisuals()
        {
            if (!showStatusTint) return;
            if (tintTarget != null && !_capturedBase)
            {
                _baseColor = tintTarget.color;
                _capturedBase = true;
            }

            if (tintTarget != null)
            {
                Color c = _baseColor;
                if (HasBurn) c = Color.Lerp(c, new Color(1f, 0.45f, 0.15f), 0.45f);
                if (HasSlow) c = Color.Lerp(c, new Color(0.45f, 0.75f, 1f), 0.4f);
                tintTarget.color = c;
            }

            EnsureWardRing();
            if (_wardRing != null)
            {
                bool on = HasWard;
                _wardRing.SetActive(on);
                if (on && _wardSr != null)
                {
                    float a = 0.25f + 0.15f * Mathf.Abs(Mathf.Sin(Time.time * 4f));
                    _wardSr.color = new Color(0.45f, 0.85f, 1f, a);
                    float scale = 1.2f + 0.15f * Mathf.Clamp01(WardAbsorbRemaining / 30f);
                    _wardRing.transform.localScale = Vector3.one * scale;
                }
            }
        }

        private void EnsureWardRing()
        {
            if (_wardRing != null) return;
            _wardRing = new GameObject("WardRing");
            _wardRing.transform.SetParent(transform);
            _wardRing.transform.localPosition = Vector3.zero;
            _wardSr = _wardRing.AddComponent<SpriteRenderer>();
            _wardSr.sprite = EnemySpriteLoader.CreatePlaceholder(new Color(0.4f, 0.85f, 1f), 48);
            _wardSr.sortingLayerName = SortingLayers.SkillEffectsBehind;
            _wardSr.sortingOrder = 2;
            _wardRing.SetActive(false);
        }

        private void RestoreTint()
        {
            if (tintTarget != null && _capturedBase)
                tintTarget.color = _baseColor;
            if (_wardRing != null)
                _wardRing.SetActive(false);
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
