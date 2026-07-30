using RealmShards.Core;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Applies hub loadout ability IDs onto a player's AbilityCaster.
    /// Empty slots clear prefab defaults so unequipped dashes/spells cannot fire.
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

            // Always rewrite every slot so leftover prefab defaults (e.g. Blink) cannot remain.
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
        }
    }
}
