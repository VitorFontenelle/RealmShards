using RealmShards.Core;
using RealmShards.Input;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Applies hub loadout ability IDs onto a player's AbilityCaster.
    /// </summary>
    public static class LoadoutApplier
    {
        public static void ApplyFromSession(AbilityCaster caster)
        {
            if (caster == null) return;
            var session = GameContext.Instance?.RunSession;
            var ids = session?.LoadoutAbilityIds;
            if (ids == null || ids.Count == 0)
            {
                var meta = GameContext.Instance?.Save?.Current?.meta;
                ids = meta?.equippedAbilityIds;
            }

            if (ids == null) return;
            for (int i = 0; i < AbilityCaster.SlotCount && i < ids.Count; i++)
            {
                var def = AbilityCatalog.Get(ids[i]);
                if (def != null)
                    caster.SetAbility(i, def);
            }
        }
    }
}
