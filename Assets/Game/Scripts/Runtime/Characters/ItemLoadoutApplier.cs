using RealmShards.Core;
using RealmShards.Progression;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Applies the lobby item chest selection onto a player's inventory at run start.
    /// </summary>
    public static class ItemLoadoutApplier
    {
        public static void ApplyFromSession(PlayerInventory inventory, int playerIndex = 0)
        {
            if (inventory == null)
                return;

            string itemId = null;
            var session = GameContext.Instance?.RunSession;
            if (session?.SelectedItemIdsByPlayer != null && playerIndex < session.SelectedItemIdsByPlayer.Count)
                itemId = session.SelectedItemIdsByPlayer[playerIndex];

            if (string.IsNullOrEmpty(itemId))
            {
                var meta = GameContext.Instance?.Save?.Current?.meta;
                if (meta != null)
                    itemId = PlayerItemLoadoutService.GetSelectedItem(meta, playerIndex);
            }

            if (string.IsNullOrEmpty(itemId))
                return;

            var def = ItemCatalog.Get(itemId);
            if (def == null)
            {
                Debug.LogWarning($"[ItemLoadout] Item id '{itemId}' not found in catalog for P{playerIndex + 1}.");
                return;
            }

            inventory.TryAdd(def);
        }
    }
}
