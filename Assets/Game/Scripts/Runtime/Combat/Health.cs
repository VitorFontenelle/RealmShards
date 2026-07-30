using System;
using UnityEngine;

namespace RealmShards
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float iFrameDuration = 0.35f;
        [SerializeField] private FactionMember factionMember;
        [SerializeField] private Rigidbody2D knockbackBody;
        [SerializeField] private float knockbackResistance = 1f;

        private float _current;
        private float _iFrameTimer;
        private bool _dead;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => _current;
        public bool IsAlive => !_dead && _current > 0f;
        public bool IsInvulnerable => _iFrameTimer > 0f;
        public FactionId Faction => factionMember != null ? factionMember.Faction : FactionId.Player;
        public int TeamId => factionMember != null ? factionMember.TeamId : 0;

        public event Action<Health, DamageInfo> Damaged;
        public event Action<Health> Died;
        public event Action<Health> Revived;

        private void Awake()
        {
            if (factionMember == null)
            {
                factionMember = GetComponent<FactionMember>();
            }

            if (knockbackBody == null)
            {
                knockbackBody = GetComponent<Rigidbody2D>();
            }

            _current = maxHealth;
        }

        private void Update()
        {
            if (_iFrameTimer > 0f)
            {
                _iFrameTimer -= Time.deltaTime;
            }
        }

        public void Configure(float newMaxHealth, float newIFrameDuration)
        {
            maxHealth = Mathf.Max(1f, newMaxHealth);
            iFrameDuration = Mathf.Max(0f, newIFrameDuration);
            if (!_dead)
            {
                _current = maxHealth;
            }
        }

        public void AddMaxHealth(float amount, bool healToFull = false)
        {
            maxHealth = Mathf.Max(1f, maxHealth + amount);
            if (healToFull)
            {
                _current = maxHealth;
            }
            else
            {
                _current = Mathf.Min(_current, maxHealth);
            }
        }

        public void FullHeal()
        {
            _current = maxHealth;
            _dead = false;
            _iFrameTimer = 0f;
            Revived?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (_dead || amount <= 0f)
                return;
            _current = Mathf.Min(maxHealth, _current + amount);
        }

        public void PulseIFrames(float duration)
        {
            _iFrameTimer = Mathf.Max(_iFrameTimer, duration);
        }

        public void TakeDamage(float amount, GameObject source)
        {
            FactionMember sourceFaction = source != null ? source.GetComponentInParent<FactionMember>() : null;
            var info = DamageInfo.Create(
                amount,
                Vector2.zero,
                transform.position,
                sourceFaction,
                source);
            TryApplyDamage(in info);
        }

        public bool TryApplyDamage(in DamageInfo damage)
        {
            if (!IsAlive)
            {
                return false;
            }

            if (!damage.IgnoreIFrames && IsInvulnerable)
            {
                return false;
            }

            if (factionMember != null && damage.Source != null)
            {
                var sourceFaction = damage.Source.GetComponentInParent<FactionMember>();
                if (sourceFaction != null && !sourceFaction.CanHarm(factionMember))
                {
                    return false;
                }
            }
            else if (!damage.IgnoreIFrames && factionMember != null)
            {
                if (damage.SourceFaction == factionMember.Faction &&
                    damage.SourceTeamId == factionMember.TeamId &&
                    !factionMember.FriendlyFire)
                {
                    return false;
                }
            }

            float incoming = Mathf.Max(0f, damage.Amount);
            var statusHost = GetComponent<Magic.StatusEffectHost>();
            if (statusHost != null)
                incoming = statusHost.AbsorbDamage(incoming);

            if (incoming <= 0f)
            {
                _iFrameTimer = iFrameDuration;
                return true;
            }

            _current = Mathf.Max(0f, _current - incoming);
            _iFrameTimer = iFrameDuration;

            if (knockbackBody != null && damage.Knockback.sqrMagnitude > 0.0001f)
            {
                float resist = Mathf.Max(0.05f, knockbackResistance);
                knockbackBody.AddForce(damage.Knockback / resist, ForceMode2D.Impulse);
            }

            Damaged?.Invoke(this, damage);

            if (incoming >= 1f)
            {
                bool heavy = incoming >= 18f;
                Combat.DamageNumberService.Spawn(transform.position, incoming, heavy);
                if (heavy)
                    Combat.HitStop.Request(0.05f);
                Audio.AudioEventHub.Play(heavy ? "combat.heavy_hit" : "combat.hit", transform.position);
            }

            if (_current <= 0f)
            {
                _dead = true;
                Audio.AudioEventHub.Play("enemy.death", transform.position);
                Died?.Invoke(this);
            }

            return true;
        }

        public void Kill()
        {
            if (_dead)
            {
                return;
            }

            _current = 0f;
            _dead = true;
            Died?.Invoke(this);
        }
    }
}
