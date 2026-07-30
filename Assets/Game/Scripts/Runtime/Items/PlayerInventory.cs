using System;
using System.Collections.Generic;
using RealmShards.Core;
using UnityEngine;

namespace RealmShards
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int capacity = 6;
        [SerializeField] private Health health;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private PlayerItemModifiers modifiers;
        [SerializeField] private GameObject pickupPrefab;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public int Capacity => capacity;
        public int Count => _items.Count;
        public IReadOnlyList<ItemDefinition> Items => _items;
        public PlayerItemModifiers Modifiers => modifiers;

        public event Action<ItemDefinition> ItemAdded;
        public event Action<ItemDefinition> ItemRemoved;

        private void Awake()
        {
            CacheRefs();
        }

        public void Configure(int newCapacity, GameObject pickup)
        {
            capacity = Mathf.Max(1, newCapacity);
            pickupPrefab = pickup;
            CacheRefs();
        }

        public bool CanAdd => _items.Count < capacity;

        public bool TryAdd(ItemDefinition item)
        {
            if (item == null || !CanAdd)
                return false;

            _items.Add(item);
            ApplyOnPickup(item);
            ItemAdded?.Invoke(item);
            return true;
        }

        public bool TryDropLast(out ItemDefinition dropped)
        {
            dropped = null;
            if (_items.Count == 0)
                return false;
            return TryDrop(_items.Count - 1, out dropped);
        }

        public bool TryDrop(int index, out ItemDefinition dropped)
        {
            dropped = null;
            if (index < 0 || index >= _items.Count)
                return false;

            dropped = _items[index];
            _items.RemoveAt(index);
            RemoveEffects(dropped);
            ItemRemoved?.Invoke(dropped);
            SpawnPickup(dropped);
            return true;
        }

        private void ApplyOnPickup(ItemDefinition item)
        {
            CacheRefs();
            modifiers?.ApplyItem(item);

            if (health != null && item.MaxHealthBonus != 0f)
                health.AddMaxHealth(item.MaxHealthBonus, healToFull: true);

            if (motor != null && item.MoveSpeedBonus != 0f)
                motor.AddMoveSpeedBonus(item.MoveSpeedBonus);

            if (item.Kind == ItemKind.EventTrigger && health != null)
            {
                if (item.HealAmount > 0f)
                {
                    if (item.HealAmount >= 900f)
                        health.FullHeal();
                    else
                        health.Heal(item.HealAmount);
                }

                if (item.GrantIFrames)
                    health.PulseIFrames(item.IFrameDuration);
            }
        }

        private void RemoveEffects(ItemDefinition item)
        {
            CacheRefs();
            modifiers?.RemoveItem(item);

            if (health != null && item.MaxHealthBonus != 0f)
                health.AddMaxHealth(-item.MaxHealthBonus, healToFull: false);

            if (motor != null && item.MoveSpeedBonus != 0f)
                motor.AddMoveSpeedBonus(-item.MoveSpeedBonus);
        }

        private void SpawnPickup(ItemDefinition item)
        {
            if (pickupPrefab == null || item == null)
                return;

            Vector2 pos = (Vector2)transform.position + Vector2.down * 0.35f;
            var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
            go.GetComponent<ItemPickup>()?.Setup(item);
        }

        /// <summary>Called by combat when this player's hit lands.</summary>
        public void NotifyPlayerDealtDamage(in DamageInfo info, Health victim)
        {
            if (modifiers == null || victim == null)
                return;

            if (modifiers.OnHitHeal > 0f && health != null)
                health.Heal(modifiers.OnHitHeal);

            if (modifiers.OnHitVestigeChance > 0f &&
                UnityEngine.Random.value <= modifiers.OnHitVestigeChance)
            {
                var ctx = GameContext.Instance;
                if (ctx != null)
                    ctx.Progression.AddArcaneVestiges(modifiers.OnHitVestigeAmount, saveImmediately: false);
            }
        }

        private void CacheRefs()
        {
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (abilityCaster == null) abilityCaster = GetComponent<AbilityCaster>();
            if (modifiers == null)
            {
                modifiers = GetComponent<PlayerItemModifiers>();
                if (modifiers == null)
                    modifiers = gameObject.AddComponent<PlayerItemModifiers>();
            }
        }
    }
}
