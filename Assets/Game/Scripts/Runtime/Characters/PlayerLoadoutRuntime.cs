using RealmShards.Save;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Per-player spell tiers applied during a run.
    /// </summary>
    public sealed class PlayerLoadoutRuntime : MonoBehaviour
    {
        [SerializeField] private int playerIndex;
        private readonly AbilityPowerTier[] _tiers = new AbilityPowerTier[4];

        public int PlayerIndex => playerIndex;

        public void Configure(int index, PlayerLoadoutData loadout, bool ultimateSlotUnlocked)
        {
            playerIndex = index;
            _tiers[0] = AbilityPowerTier.Signature;
            _tiers[1] = AbilityPowerTier.Signature;
            _tiers[2] = loadout != null
                ? loadout.GetTier(AbilitySlotRole.Signature)
                : AbilityPowerTier.Signature;
            _tiers[3] = ultimateSlotUnlocked && loadout != null && !string.IsNullOrEmpty(loadout.ultimateId)
                ? AbilityPowerTier.Ultimate
                : AbilityPowerTier.Signature;
        }

        public float GetDamageMultiplier(int slot)
        {
            slot = Mathf.Clamp(slot, 0, 3);
            return _tiers[slot] switch
            {
                AbilityPowerTier.Optimized => 1.35f,
                AbilityPowerTier.Ultimate => 1.75f,
                _ => 1f
            };
        }

        public bool TryUpgradeSignatureToOptimized()
        {
            int tier = (int)_tiers[2];
            if (tier >= (int)AbilityPowerTier.Optimized)
                return false;
            _tiers[2] = AbilityPowerTier.Optimized;
            return true;
        }
    }
}
