using System;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Aggregates stacked item buffs for a player. Queried by AbilityCaster / inventory / magnet.
    /// </summary>
    public sealed class PlayerItemModifiers : MonoBehaviour
    {
        private float _damageMultiplier = 1f;
        private float _cooldownMultiplier = 1f;
        private float _abilityDamageFlat;
        private bool _boltPierce;
        private int _boltSplitExtra;
        private float _pulseRadiusBonus;
        private float _blinkDistanceBonus;
        private float _pickupMagnetRadius;
        private float _onHitHeal;
        private float _onHitVestigeChance;
        private int _onHitVestigeAmount = 1;
        private float _moveSpeedBonus;
        private float _maxHealthBonusApplied;

        public float DamageMultiplier => Mathf.Max(0.05f, _damageMultiplier);
        public float CooldownMultiplier => Mathf.Clamp(_cooldownMultiplier, 0.25f, 2f);
        public float AbilityDamageFlat => _abilityDamageFlat;
        public bool BoltPierce => _boltPierce;
        public int BoltSplitExtra => Mathf.Max(0, _boltSplitExtra);
        public float PulseRadiusBonus => _pulseRadiusBonus;
        public float BlinkDistanceBonus => _blinkDistanceBonus;
        public float PickupMagnetRadius => _pickupMagnetRadius;
        public float OnHitHeal => _onHitHeal;
        public float OnHitVestigeChance => _onHitVestigeChance;
        public int OnHitVestigeAmount => _onHitVestigeAmount;
        public float MoveSpeedBonus => _moveSpeedBonus;

        public event Action ModifiersChanged;

        public void ResetAll()
        {
            _damageMultiplier = 1f;
            _cooldownMultiplier = 1f;
            _abilityDamageFlat = 0f;
            _boltPierce = false;
            _boltSplitExtra = 0;
            _pulseRadiusBonus = 0f;
            _blinkDistanceBonus = 0f;
            _pickupMagnetRadius = 0f;
            _onHitHeal = 0f;
            _onHitVestigeChance = 0f;
            _onHitVestigeAmount = 1;
            _moveSpeedBonus = 0f;
            _maxHealthBonusApplied = 0f;
            ModifiersChanged?.Invoke();
        }

        public void ApplyItem(ItemDefinition item)
        {
            if (item == null) return;
            ApplyDelta(item, +1);
        }

        public void RemoveItem(ItemDefinition item)
        {
            if (item == null) return;
            ApplyDelta(item, -1);
        }

        private void ApplyDelta(ItemDefinition item, int sign)
        {
            _damageMultiplier += item.DamageMultiplierBonus * sign;
            if (item.CooldownMultiplier > 0f && !Mathf.Approximately(item.CooldownMultiplier, 1f))
            {
                if (sign > 0)
                    _cooldownMultiplier *= item.CooldownMultiplier;
                else if (item.CooldownMultiplier != 0f)
                    _cooldownMultiplier /= item.CooldownMultiplier;
            }

            _abilityDamageFlat += item.AbilityDamageFlatBonus * sign;
            if (item.GrantBoltPierce)
                _boltPierce = sign > 0 || CountPierceSources() > 0;
            _boltSplitExtra += item.BoltSplitExtraProjectiles * sign;
            _pulseRadiusBonus += item.PulseRadiusBonus * sign;
            _blinkDistanceBonus += item.BlinkDistanceBonus * sign;
            _pickupMagnetRadius += item.PickupMagnetRadius * sign;
            _onHitHeal += item.OnHitHeal * sign;
            _onHitVestigeChance += item.OnHitVestigeChance * sign;
            if (item.OnHitVestigeAmount > 0 && sign > 0)
                _onHitVestigeAmount = Mathf.Max(_onHitVestigeAmount, item.OnHitVestigeAmount);
            _moveSpeedBonus += item.MoveSpeedBonus * sign;
            _maxHealthBonusApplied += item.MaxHealthBonus * sign;
            ModifiersChanged?.Invoke();
        }

        private int CountPierceSources()
        {
            // Soft flag; inventory re-syncs on rebuild. Keep pierce if multiplier path set it.
            return _boltPierce ? 1 : 0;
        }

        public float ScaleDamage(float baseDamage)
        {
            return (baseDamage + _abilityDamageFlat) * DamageMultiplier;
        }

        public float ScaleCooldown(float baseCooldown)
        {
            return baseCooldown * CooldownMultiplier;
        }

        public float ScalePulseRadius(float baseRadius)
        {
            return baseRadius + _pulseRadiusBonus;
        }

        public float ScaleBlinkDistance(float baseDistance)
        {
            return baseDistance + _blinkDistanceBonus;
        }
    }
}
