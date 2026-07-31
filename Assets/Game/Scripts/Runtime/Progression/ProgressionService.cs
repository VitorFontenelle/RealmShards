using System;
using UnityEngine;

namespace RealmShards.Progression
{
    /// <summary>
    /// Year / decade calendar API for meta progression.
    /// Failure advances year by +10 (one decade).
    /// </summary>
    public sealed class ProgressionService
    {
        private readonly Save.ISaveService _save;

        public ProgressionService(Save.ISaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
        }

        public int Year => _save.Current.meta.year;
        public int Decade => _save.Current.meta.decade;
        public int ArcaneVestiges => _save.Current.meta.arcaneVestiges;
        public int Vials => _save.Current.meta.vials;

        public event Action<int, int> YearChanged;

        public void SetYear(int year, bool saveImmediately = true)
        {
            var meta = _save.Current.meta;
            meta.year = Mathf.Max(0, year);
            meta.decade = meta.year / 10;
            if (saveImmediately)
            {
                _save.Save();
            }

            YearChanged?.Invoke(meta.year, meta.decade);
        }

        /// <summary>Advances calendar by one decade (+10 years). Called on run failure.</summary>
        public void AdvanceDecadeOnFailure(bool saveImmediately = true)
        {
            SetYear(Year + 10, saveImmediately);
        }

        public void AddArcaneVestiges(int amount, bool saveImmediately = true)
        {
            if (amount == 0)
            {
                return;
            }

            var meta = _save.Current.meta;
            meta.arcaneVestiges = Mathf.Max(0, meta.arcaneVestiges + amount);
            if (saveImmediately)
            {
                _save.Save();
            }
        }

        public void AddVials(int amount, bool saveImmediately = true)
        {
            if (amount == 0)
                return;

            var meta = _save.Current.meta;
            meta.vials = Mathf.Max(0, meta.vials + amount);
            if (saveImmediately)
                _save.Save();
        }

        public bool TrySpendVials(int cost, out string failReason)
        {
            failReason = null;
            if (cost < 0)
            {
                failReason = "Invalid cost.";
                return false;
            }

            if (Vials < cost)
            {
                failReason = "Not enough vials.";
                return false;
            }

            _save.Current.meta.vials -= cost;
            _save.Save();
            return true;
        }

        public bool IsAbilityUnlocked(string abilityId)
        {
            return !string.IsNullOrEmpty(abilityId)
                   && _save.Current.meta.unlockedAbilityIds.Contains(abilityId);
        }

        public void UnlockAbility(string abilityId, bool saveImmediately = true)
        {
            if (string.IsNullOrEmpty(abilityId))
                return;

            var list = _save.Current.meta.unlockedAbilityIds;
            if (!list.Contains(abilityId))
            {
                list.Add(abilityId);
                if (saveImmediately)
                    _save.Save();
            }
        }

        public bool IsItemUnlocked(string itemId) =>
            PlayerItemLoadoutService.IsItemUnlocked(_save.Current.meta, itemId);

        public void UnlockItem(string itemId, bool saveImmediately = true)
        {
            if (string.IsNullOrEmpty(itemId))
                return;

            var list = _save.Current.meta.unlockedItemIds;
            list ??= _save.Current.meta.unlockedItemIds = new System.Collections.Generic.List<string>();
            if (!list.Contains(itemId))
            {
                list.Add(itemId);
                if (saveImmediately)
                    _save.Save();
            }
        }

        /// <summary>
        /// Spend Arcane Vestiges to permanently unlock an ability. No duplicate spend.
        /// </summary>
        public bool TryPurchaseAbilityUnlock(string abilityId, int cost, out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(abilityId))
            {
                failReason = "Invalid ability.";
                return false;
            }

            if (IsAbilityUnlocked(abilityId))
            {
                failReason = "Already unlocked.";
                return false;
            }

            if (cost < 0)
            {
                failReason = "Invalid cost.";
                return false;
            }

            if (ArcaneVestiges < cost)
            {
                failReason = "Not enough Arcane Vestiges.";
                return false;
            }

            var meta = _save.Current.meta;
            meta.arcaneVestiges -= cost;
            if (!meta.unlockedAbilityIds.Contains(abilityId))
                meta.unlockedAbilityIds.Add(abilityId);
            _save.Save();
            return true;
        }

        public void SetEquippedAbility(int slot, string abilityId, bool saveImmediately = true)
        {
            var list = _save.Current.meta.equippedAbilityIds;
            while (list.Count < 4)
                list.Add(string.Empty);
            slot = Mathf.Clamp(slot, 0, 3);
            list[slot] = abilityId ?? string.Empty;
            if (saveImmediately)
                _save.Save();
        }
    }
}
