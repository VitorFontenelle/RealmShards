using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealmShards
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int capacity = 6;
        [SerializeField] private Health health;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private AbilityCaster abilityCaster;
        [SerializeField] private GameObject pickupPrefab;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public int Capacity => capacity;
        public int Count => _items.Count;
        public IReadOnlyList<ItemDefinition> Items => _items;

        public event Action<ItemDefinition> ItemAdded;
        public event Action<ItemDefinition> ItemRemoved;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (abilityCaster == null) abilityCaster = GetComponent<AbilityCaster>();
        }

        public void Configure(int newCapacity, GameObject pickup)
        {
            capacity = Mathf.Max(1, newCapacity);
            pickupPrefab = pickup;
        }

        public bool CanAdd => _items.Count < capacity;

        public bool TryAdd(ItemDefinition item)
        {
            if (item == null || !CanAdd)
            {
                return false;
            }

            _items.Add(item);
            ApplyOnPickup(item);
            ItemAdded?.Invoke(item);
            return true;
        }

        public bool TryDropLast(out ItemDefinition dropped)
        {
            dropped = null;
            if (_items.Count == 0)
            {
                return false;
            }

            int last = _items.Count - 1;
            dropped = _items[last];
            _items.RemoveAt(last);
            RemoveEffects(dropped);
            ItemRemoved?.Invoke(dropped);
            SpawnPickup(dropped);
            return true;
        }

        public bool TryDrop(int index, out ItemDefinition dropped)
        {
            dropped = null;
            if (index < 0 || index >= _items.Count)
            {
                return false;
            }

            dropped = _items[index];
            _items.RemoveAt(index);
            RemoveEffects(dropped);
            ItemRemoved?.Invoke(dropped);
            SpawnPickup(dropped);
            return true;
        }

        private void ApplyOnPickup(ItemDefinition item)
        {
            switch (item.Kind)
            {
                case ItemKind.StatBoost:
                    if (health != null && item.MaxHealthBonus != 0f)
                    {
                        health.AddMaxHealth(item.MaxHealthBonus, healToFull: true);
                    }

                    if (motor != null && item.MoveSpeedBonus != 0f)
                    {
                        motor.AddMoveSpeedBonus(item.MoveSpeedBonus);
                    }
                    break;

                case ItemKind.EventTrigger:
                    if (health != null)
                    {
                        if (item.HealAmount > 0f)
                        {
                            health.FullHeal();
                        }

                        if (item.GrantIFrames)
                        {
                            health.PulseIFrames(item.IFrameDuration);
                        }
                    }
                    break;

                case ItemKind.AbilityModifier:
                    // Runtime mods tracked lightly via motor/caster speed tint; full ability cloning omitted for Stage 2.
                    if (motor != null)
                    {
                        motor.AddMoveSpeedBonus(0.15f);
                    }
                    break;
            }
        }

        private void RemoveEffects(ItemDefinition item)
        {
            if (item.Kind == ItemKind.StatBoost && motor != null && item.MoveSpeedBonus != 0f)
            {
                motor.AddMoveSpeedBonus(-item.MoveSpeedBonus);
            }
        }

        private void SpawnPickup(ItemDefinition item)
        {
            if (pickupPrefab == null || item == null)
            {
                return;
            }

            Vector2 pos = (Vector2)transform.position + Vector2.down * 0.35f;
            var go = Instantiate(pickupPrefab, pos, Quaternion.identity);
            var pickup = go.GetComponent<ItemPickup>();
            pickup?.Setup(item);
        }
    }
}
