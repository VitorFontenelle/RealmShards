using System.Collections.Generic;
using RealmShards.Save;

namespace RealmShards.Progression
{
    /// <summary>
    /// Hub lobby items vendor: surfaces unowned catalog items and grants free unlocks to the chest.
    /// </summary>
    public static class ItemsVendorService
    {
        public const int MaxDisplayedItems = 3;

        public static List<string> GetOfferedItemIds(MetaProgressionData meta)
        {
            var result = new List<string>(MaxDisplayedItems);
            if (meta == null)
                return result;

            var ordered = new List<string>();
            foreach (var def in ItemCatalog.All)
            {
                if (def == null || string.IsNullOrEmpty(def.ContentId))
                    continue;
                ordered.Add(def.ContentId);
            }

            ordered.Sort(System.StringComparer.Ordinal);

            for (int i = 0; i < ordered.Count; i++)
            {
                string id = ordered[i];
                if (PlayerItemLoadoutService.IsItemUnlocked(meta, id))
                    continue;
                result.Add(id);
                if (result.Count >= MaxDisplayedItems)
                    break;
            }

            return result;
        }

        public static bool TryClaimItem(string itemId, ISaveService save, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(itemId))
            {
                failReason = "Invalid item.";
                return false;
            }

            if (save?.Current?.meta == null)
            {
                failReason = "Save unavailable.";
                return false;
            }

            if (PlayerItemLoadoutService.IsItemUnlocked(save.Current.meta, itemId))
            {
                failReason = "Already owned.";
                return false;
            }

            if (ItemCatalog.Get(itemId) == null)
            {
                failReason = "Unknown item.";
                return false;
            }

            var offered = GetOfferedItemIds(save.Current.meta);
            if (!offered.Contains(itemId))
            {
                failReason = "Not on display.";
                return false;
            }

            new ProgressionService(save).UnlockItem(itemId);
            return true;
        }
    }
}
