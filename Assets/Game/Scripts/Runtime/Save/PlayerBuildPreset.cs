using System;

namespace RealmShards.Save
{
    [Serializable]
    public sealed class PlayerBuildPreset
    {
        public string primaryId = string.Empty;
        public string dashId = string.Empty;
        public string signatureId = string.Empty;
        public string ultimateId = string.Empty;
        public string itemId = string.Empty;

        public bool IsEmpty =>
            string.IsNullOrEmpty(primaryId)
            && string.IsNullOrEmpty(dashId)
            && string.IsNullOrEmpty(signatureId)
            && string.IsNullOrEmpty(ultimateId)
            && string.IsNullOrEmpty(itemId);

        public void Clear()
        {
            primaryId = string.Empty;
            dashId = string.Empty;
            signatureId = string.Empty;
            ultimateId = string.Empty;
            itemId = string.Empty;
        }

        public void CopyFrom(PlayerLoadoutData loadout)
        {
            if (loadout == null)
            {
                Clear();
                return;
            }

            primaryId = loadout.primaryId ?? string.Empty;
            dashId = loadout.dashId ?? string.Empty;
            signatureId = loadout.signatureId ?? string.Empty;
            ultimateId = loadout.ultimateId ?? string.Empty;
            itemId = loadout.selectedItemId ?? string.Empty;
        }

        public void ApplyTo(PlayerLoadoutData loadout)
        {
            if (loadout == null)
                return;

            loadout.primaryId = primaryId ?? string.Empty;
            loadout.dashId = dashId ?? string.Empty;
            loadout.signatureId = signatureId ?? string.Empty;
            loadout.ultimateId = ultimateId ?? string.Empty;
            loadout.selectedItemId = itemId ?? string.Empty;
            loadout.signatureTier = (int)AbilityPowerTier.Signature;
            loadout.ultimateTier = (int)AbilityPowerTier.Ultimate;
        }

        public void CopyFrom(PlayerBuildPreset other)
        {
            if (other == null)
            {
                Clear();
                return;
            }

            primaryId = other.primaryId ?? string.Empty;
            dashId = other.dashId ?? string.Empty;
            signatureId = other.signatureId ?? string.Empty;
            ultimateId = other.ultimateId ?? string.Empty;
            itemId = other.itemId ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class PlayerBuildBank
    {
        public const int SlotCount = 5;

        public PlayerBuildPreset slot0 = new PlayerBuildPreset();
        public PlayerBuildPreset slot1 = new PlayerBuildPreset();
        public PlayerBuildPreset slot2 = new PlayerBuildPreset();
        public PlayerBuildPreset slot3 = new PlayerBuildPreset();
        public PlayerBuildPreset slot4 = new PlayerBuildPreset();

        public PlayerBuildPreset GetSlot(int index) => index switch
        {
            0 => slot0,
            1 => slot1,
            2 => slot2,
            3 => slot3,
            4 => slot4,
            _ => slot0
        };
    }
}
