using System;
using System.Collections.Generic;

namespace RealmShards.Save
{
    [Serializable]
    public sealed class PlayerLoadoutData
    {
        public string primaryId = ContentIdDefaults.AbilityBasicBolt;
        public string dashId = ContentIdDefaults.AbilityDash;
        public string signatureId = string.Empty;
        public string ultimateId = string.Empty;
        public string selectedItemId = string.Empty;
        public int signatureTier = (int)AbilityPowerTier.Signature;
        public int ultimateTier = (int)AbilityPowerTier.Ultimate;

        public string GetAbilityId(AbilitySlotRole role) => role switch
        {
            AbilitySlotRole.Primary => primaryId,
            AbilitySlotRole.Dash => dashId,
            AbilitySlotRole.Signature => signatureId,
            AbilitySlotRole.Ultimate => ultimateId,
            _ => string.Empty
        };

        public void SetAbilityId(AbilitySlotRole role, string abilityId)
        {
            switch (role)
            {
                case AbilitySlotRole.Primary: primaryId = abilityId ?? string.Empty; break;
                case AbilitySlotRole.Dash: dashId = abilityId ?? string.Empty; break;
                case AbilitySlotRole.Signature:
                    signatureId = abilityId ?? string.Empty;
                    signatureTier = (int)AbilityPowerTier.Signature;
                    break;
                case AbilitySlotRole.Ultimate:
                    ultimateId = abilityId ?? string.Empty;
                    ultimateTier = (int)AbilityPowerTier.Ultimate;
                    break;
            }
        }

        public AbilityPowerTier GetTier(AbilitySlotRole role) => role switch
        {
            AbilitySlotRole.Signature => (AbilityPowerTier)Math.Max(0, signatureTier),
            AbilitySlotRole.Ultimate => (AbilityPowerTier)Math.Max((int)AbilityPowerTier.Ultimate, ultimateTier),
            _ => AbilityPowerTier.Signature
        };

        public List<string> ToEquippedList(bool ultimateSlotUnlocked)
        {
            var list = new List<string>(4)
            {
                primaryId ?? string.Empty,
                dashId ?? string.Empty,
                signatureId ?? string.Empty,
                ultimateSlotUnlocked ? ultimateId ?? string.Empty : string.Empty
            };
            return list;
        }
    }
}
