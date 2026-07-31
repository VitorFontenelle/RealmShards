using RealmShards.Core;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.Progression
{
    public static class PlayerBuildService
    {
        public const int SlotCount = PlayerBuildBank.SlotCount;
        public const int MaxPlayers = 4;

        public static void EnsureBanks(MetaProgressionData meta)
        {
            meta.playerBuildBanks ??= new System.Collections.Generic.List<PlayerBuildBank>();
            while (meta.playerBuildBanks.Count < MaxPlayers)
                meta.playerBuildBanks.Add(new PlayerBuildBank());
        }

        public static PlayerBuildBank GetBank(MetaProgressionData meta, int playerIndex)
        {
            EnsureBanks(meta);
            playerIndex = Mathf.Clamp(playerIndex, 0, MaxPlayers - 1);
            return meta.playerBuildBanks[playerIndex];
        }

        public static PlayerBuildPreset GetPreset(MetaProgressionData meta, int playerIndex, int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, SlotCount - 1);
            return GetBank(meta, playerIndex).GetSlot(slotIndex);
        }

        public static void SaveCurrentBuild(int playerIndex, int slotIndex, ISaveService save)
        {
            if (save?.Current?.meta == null) return;
            var meta = save.Current.meta;
            PlayerLoadoutService.EnsureLoadouts(meta);
            var loadout = PlayerLoadoutService.GetLoadout(meta, playerIndex);
            GetPreset(meta, playerIndex, slotIndex).CopyFrom(loadout);
            save.Save();
        }

        public static void DressBuild(int playerIndex, int slotIndex, ISaveService save)
        {
            if (save?.Current?.meta == null) return;
            var meta = save.Current.meta;
            PlayerLoadoutService.EnsureLoadouts(meta);
            var preset = GetPreset(meta, playerIndex, slotIndex);
            if (preset.IsEmpty)
                return;

            var loadout = PlayerLoadoutService.GetLoadout(meta, playerIndex);
            preset.ApplyTo(loadout);
            PlayerLoadoutService.MirrorPrimaryToLegacy(meta);
            save.Save();
        }

        public static void DeleteBuild(int playerIndex, int slotIndex, ISaveService save)
        {
            if (save?.Current?.meta == null) return;
            GetPreset(save.Current.meta, playerIndex, slotIndex).Clear();
            save.Save();
        }

        public static string DescribePreset(MetaProgressionData meta, PlayerBuildPreset preset)
        {
            if (preset == null || preset.IsEmpty)
                return "(Empty)";

            string SpellLabel(string id) =>
                string.IsNullOrEmpty(id) ? "—" : ResolveName(meta, id);

            string spells = $"{SpellLabel(preset.primaryId)} · {SpellLabel(preset.dashId)} · {SpellLabel(preset.signatureId)} · {SpellLabel(preset.ultimateId)}";
            string item = string.IsNullOrEmpty(preset.itemId) ? "No item" : ResolveName(meta, preset.itemId);
            return $"{spells}\nItem: {item}";
        }

        private static string ResolveName(MetaProgressionData meta, string id)
        {
            var ctx = GameContext.Instance;
            if (ctx != null)
            {
                string fromContent = ctx.Content.GetDisplayName(id, string.Empty);
                if (!string.IsNullOrEmpty(fromContent))
                    return fromContent;
            }

            var item = ItemCatalog.Get(id);
            if (item != null)
                return item.DisplayName;

            var ability = AbilityCatalog.Get(id);
            if (ability != null)
                return ability.DisplayName;

            return id;
        }
    }
}
