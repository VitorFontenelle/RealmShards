using System.Collections.Generic;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.Progression
{
    public static class PlayerLoadoutService
    {
        public static bool UltimateSlotUnlocked(MetaProgressionData meta) => meta != null && meta.ultimateSlotUnlocked;

        public static PlayerLoadoutData GetLoadout(MetaProgressionData meta, int playerIndex)
        {
            EnsureLoadouts(meta);
            playerIndex = Mathf.Clamp(playerIndex, 0, 3);
            return meta.playerLoadouts[playerIndex];
        }

        public static void SetAbility(int playerIndex, AbilitySlotRole role, string abilityId, ISaveService save)
        {
            if (save?.Current?.meta == null) return;
            var meta = save.Current.meta;
            EnsureLoadouts(meta);
            GetLoadout(meta, playerIndex).SetAbilityId(role, abilityId);
            MirrorPrimaryToLegacy(meta);
            save.Save();
        }

        public static List<string> GetEquippedForPlayer(MetaProgressionData meta, int playerIndex)
        {
            return GetLoadout(meta, playerIndex).ToEquippedList(UltimateSlotUnlocked(meta));
        }

        public static List<IReadOnlyList<string>> GetAllEquipped(MetaProgressionData meta, int playerCount)
        {
            var result = new List<IReadOnlyList<string>>(playerCount);
            for (int i = 0; i < playerCount; i++)
                result.Add(GetEquippedForPlayer(meta, i));
            return result;
        }

        public static IEnumerable<string> CandidatesForRole(MetaProgressionData meta, AbilitySlotRole role)
        {
            if (meta?.unlockedAbilityIds == null) yield break;
            for (int i = 0; i < meta.unlockedAbilityIds.Count; i++)
            {
                string id = meta.unlockedAbilityIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                var def = AbilityCatalog.Get(id);
                if (def == null) continue;
                if (MatchesRole(def, role))
                    yield return id;
            }
        }

        public static bool MatchesRole(AbilityDefinition def, AbilitySlotRole role)
        {
            if (def == null) return false;
            return role switch
            {
                AbilitySlotRole.Dash => def.Kind == AbilityKind.Dash,
                AbilitySlotRole.Primary => def.Kind != AbilityKind.Dash && IsPrimaryCandidate(def),
                AbilitySlotRole.Signature => def.Kind != AbilityKind.Dash && !IsPrimaryCandidate(def),
                AbilitySlotRole.Ultimate => def.Kind != AbilityKind.Dash && !IsPrimaryCandidate(def),
                _ => false
            };
        }

        private static bool IsPrimaryCandidate(AbilityDefinition def)
        {
            return def.ContentId == ContentIdDefaults.AbilityAirBullet
                   || def.ContentId == ContentIdDefaults.AbilityBasicBolt
                   || def.Cooldown <= 0.5f && def.Damage <= 18f;
        }

        public static void EnsureLoadouts(MetaProgressionData meta)
        {
            meta.playerLoadouts ??= new List<PlayerLoadoutData>();
            while (meta.playerLoadouts.Count < 4)
            {
                var loadout = new PlayerLoadoutData();
                if (meta.playerLoadouts.Count == 0 && meta.equippedAbilityIds != null && meta.equippedAbilityIds.Count >= 4)
                {
                    loadout.primaryId = meta.equippedAbilityIds[0];
                    loadout.dashId = meta.equippedAbilityIds[1];
                    loadout.signatureId = meta.equippedAbilityIds[2];
                    loadout.ultimateId = meta.equippedAbilityIds[3];
                }

                meta.playerLoadouts.Add(loadout);
            }
        }

        public static void MirrorPrimaryToLegacy(MetaProgressionData meta)
        {
            meta.equippedAbilityIds ??= new List<string>();
            var primary = GetLoadout(meta, 0).ToEquippedList(UltimateSlotUnlocked(meta));
            meta.equippedAbilityIds.Clear();
            meta.equippedAbilityIds.AddRange(primary);
            while (meta.equippedAbilityIds.Count < 4)
                meta.equippedAbilityIds.Add(string.Empty);
        }
    }
}
