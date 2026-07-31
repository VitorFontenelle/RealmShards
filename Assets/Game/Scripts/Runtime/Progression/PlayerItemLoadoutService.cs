using System.Collections.Generic;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.Progression
{
    public static class PlayerItemLoadoutService
    {
        public static string GetSelectedItem(MetaProgressionData meta, int playerIndex)
        {
            return PlayerLoadoutService.GetLoadout(meta, playerIndex).selectedItemId ?? string.Empty;
        }

        public static void SetSelectedItem(int playerIndex, string itemId, ISaveService save)
        {
            if (save?.Current?.meta == null) return;
            var meta = save.Current.meta;
            PlayerLoadoutService.EnsureLoadouts(meta);
            GetLoadout(meta, playerIndex).selectedItemId = itemId ?? string.Empty;
            save.Save();
        }

        public static void ClearSelectedItem(int playerIndex, ISaveService save) =>
            SetSelectedItem(playerIndex, string.Empty, save);

        public static List<string> GetAllSelected(MetaProgressionData meta, int playerCount)
        {
            var result = new List<string>(playerCount);
            for (int i = 0; i < playerCount; i++)
                result.Add(GetSelectedItem(meta, i));
            return result;
        }

        public static bool IsItemUnlocked(MetaProgressionData meta, string itemId)
        {
            return !string.IsNullOrEmpty(itemId)
                   && meta?.unlockedItemIds != null
                   && meta.unlockedItemIds.Contains(itemId);
        }

        public static IEnumerable<string> CandidatesForCategory(MetaProgressionData meta, ItemCategory category)
        {
            if (meta?.unlockedItemIds == null) yield break;
            for (int i = 0; i < meta.unlockedItemIds.Count; i++)
            {
                string id = meta.unlockedItemIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (ItemCatalog.Get(id) == null) continue;
                if (GetCategory(id) == category)
                    yield return id;
            }
        }

        public static ItemCategory GetCategory(string itemId)
        {
            return itemId switch
            {
                ContentIdDefaults.ItemNeedleShard => ItemCategory.Attack,
                ContentIdDefaults.ItemWideningHalo => ItemCategory.Attack,
                ContentIdDefaults.ItemChronoweave => ItemCategory.Attack,
                ContentIdDefaults.ItemGlassmarrow => ItemCategory.Attack,
                ContentIdDefaults.ItemMindthread => ItemCategory.Attack,
                ContentIdDefaults.ItemHeartward => ItemCategory.Defense,
                ContentIdDefaults.ItemIronvine => ItemCategory.Defense,
                ContentIdDefaults.ItemEmberSpark => ItemCategory.Defense,
                ContentIdDefaults.ItemLodestone => ItemCategory.Misc,
                ContentIdDefaults.ItemStridefeather => ItemCategory.Misc,
                _ => ItemCategory.Misc
            };
        }

        public static string CategoryLabel(ItemCategory category) => category switch
        {
            ItemCategory.Attack => "ATTACK",
            ItemCategory.Defense => "DEFENSE",
            ItemCategory.Misc => "MISC",
            _ => category.ToString().ToUpperInvariant()
        };

        private static PlayerLoadoutData GetLoadout(MetaProgressionData meta, int playerIndex) =>
            PlayerLoadoutService.GetLoadout(meta, playerIndex);
    }
}
