using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Applies hub loadout ability IDs onto a player's AbilityCaster.
    /// Empty slots clear prefab defaults so unequipped dashes/spells cannot fire.
    /// </summary>
    public static class LoadoutApplier
    {
        public static void ApplyFromSession(AbilityCaster caster, int playerIndex = 0)
        {
            if (caster == null) return;
            var session = GameContext.Instance?.RunSession;
            var ids = session?.LoadoutsByPlayer != null && playerIndex < session.LoadoutsByPlayer.Count
                ? session.LoadoutsByPlayer[playerIndex]
                : null;
            if (ids == null || ids.Count == 0)
            {
                ids = session?.LoadoutAbilityIds;
            }

            if (ids == null || ids.Count == 0)
            {
                var meta = GameContext.Instance?.Save?.Current?.meta;
                ids = meta != null
                    ? PlayerLoadoutService.GetEquippedForPlayer(meta, playerIndex)
                    : meta?.equippedAbilityIds;
            }

            for (int i = 0; i < AbilityCaster.SlotCount; i++)
            {
                string id = ids != null && i < ids.Count ? ids[i] : string.Empty;
                if (string.IsNullOrEmpty(id))
                {
                    caster.SetAbility(i, null);
                    continue;
                }

                var def = AbilityCatalog.Get(id);
                caster.SetAbility(i, def);
                if (def == null)
                    Debug.LogWarning($"[Loadout] Ability id '{id}' not found in catalog for slot {i}.");
            }

            var runtime = caster.GetComponent<PlayerLoadoutRuntime>();
            if (runtime == null)
                runtime = caster.gameObject.AddComponent<PlayerLoadoutRuntime>();
            var loadout = GameContext.Instance?.Save?.Current?.meta != null
                ? PlayerLoadoutService.GetLoadout(GameContext.Instance.Save.Current.meta, playerIndex)
                : null;
            runtime.Configure(
                playerIndex,
                loadout,
                PlayerLoadoutService.UltimateSlotUnlocked(GameContext.Instance?.Save?.Current?.meta));
        }
    }
}
