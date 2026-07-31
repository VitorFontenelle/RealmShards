using System.Collections;
using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Enemies;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Seated items vendor with a display cloth showing up to three unowned items.
    /// </summary>
    public sealed class ItemsVendorDisplay : MonoBehaviour
    {
        private const float WorldScale = 0.55f;
        private const float SpritePpu = 200f;
        private const float ItemInteractRange = 1.35f;
        private static readonly Vector3 ClothOffset = new Vector3(1.55f, -0.12f, 0f);
        private static readonly Vector3[] ItemOffsets =
        {
            new Vector3(-0.52f, 0.12f, 0f),
            new Vector3(0f, 0.16f, 0f),
            new Vector3(0.52f, 0.12f, 0f)
        };

        private sealed class ItemSlotView
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public BoxCollider2D Collider;
            public string ItemId;
            public Vector3 BasePosition;
            public Coroutine BobRoutine;
        }

        private SpriteRenderer _vendorRenderer;
        private SpriteRenderer _clothRenderer;
        private readonly ItemSlotView[] _slots = new ItemSlotView[ItemsVendorService.MaxDisplayedItems];

        public static ItemsVendorDisplay Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("ItemsVendor");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * WorldScale;
            go.layer = GameLayers.Environment;
            var display = go.AddComponent<ItemsVendorDisplay>();
            display.BuildVisuals();
            return display;
        }

        public void RefreshDisplay(MetaProgressionData meta)
        {
            var offered = ItemsVendorService.GetOfferedItemIds(meta);
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot == null)
                    continue;

                if (i < offered.Count)
                    ShowSlot(slot, offered[i]);
                else
                    HideSlot(slot);
            }
        }

        public bool TryResolveItemInteract(
            IReadOnlyList<(Vector3 position, bool actionPressed)> players,
            out string itemId,
            out int slotIndex)
        {
            itemId = null;
            slotIndex = -1;
            float bestDistance = float.MaxValue;

            for (int s = 0; s < _slots.Length; s++)
            {
                var slot = _slots[s];
                if (slot?.Root == null || !slot.Root.activeSelf || string.IsNullOrEmpty(slot.ItemId))
                    continue;

                Vector3 interactPoint = slot.Root.transform.position;
                for (int p = 0; p < players.Count; p++)
                {
                    var player = players[p];
                    if (!player.actionPressed)
                        continue;

                    float distance = Vector2.Distance(player.position, interactPoint);
                    if (distance > ItemInteractRange || distance >= bestDistance)
                        continue;

                    bestDistance = distance;
                    itemId = slot.ItemId;
                    slotIndex = s;
                }
            }

            return slotIndex >= 0;
        }

        public void PlayClaimEffect(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length)
                return;

            var slot = _slots[slotIndex];
            if (slot?.Root == null)
                return;

            StartCoroutine(ClaimEffectRoutine(slot));
        }

        private void BuildVisuals()
        {
            _vendorRenderer = CreateSpriteChild("Vendor", Vector3.zero, LoadVendorSprite(), new Vector2(0.5f, 0f), 11);
            _clothRenderer = CreateSpriteChild("Cloth", ClothOffset, LoadClothSprite(), new Vector2(0.5f, 0.5f), 9);

            for (int i = 0; i < _slots.Length; i++)
            {
                var slotRoot = new GameObject($"ItemSlot_{i + 1}");
                slotRoot.transform.SetParent(transform, false);
                slotRoot.transform.localPosition = ClothOffset + ItemOffsets[i];

                var renderer = slotRoot.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = SortingLayers.EnvironmentFront;
                renderer.sortingOrder = 13;

                var collider = slotRoot.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                collider.size = new Vector2(0.55f, 0.55f);

                _slots[i] = new ItemSlotView
                {
                    Root = slotRoot,
                    Renderer = renderer,
                    Collider = collider,
                    BasePosition = slotRoot.transform.localPosition
                };
                slotRoot.SetActive(false);
            }
        }

        private SpriteRenderer CreateSpriteChild(string name, Vector3 localPosition, Sprite sprite, Vector2 pivot, int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = localPosition;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite ?? EnemySpriteLoader.CreatePlaceholder(new Color(0.35f, 0.2f, 0.45f), 64);
            renderer.sortingLayerName = SortingLayers.EnvironmentFront;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ShowSlot(ItemSlotView slot, string itemId)
        {
            var def = ItemCatalog.Get(itemId);
            slot.ItemId = itemId;
            slot.Root.SetActive(true);
            slot.Renderer.sprite = def?.Icon ?? EnemySpriteLoader.CreatePlaceholder(new Color(0.75f, 0.65f, 0.35f), 48);
            slot.Renderer.color = def != null ? def.Tint : Color.white;
            slot.Root.transform.localPosition = slot.BasePosition;
            slot.Renderer.transform.localScale = Vector3.one * 0.42f;

            if (slot.BobRoutine != null)
                StopCoroutine(slot.BobRoutine);
            slot.BobRoutine = StartCoroutine(BobRoutine(slot));
        }

        private void HideSlot(ItemSlotView slot)
        {
            slot.ItemId = null;
            if (slot.BobRoutine != null)
            {
                StopCoroutine(slot.BobRoutine);
                slot.BobRoutine = null;
            }

            slot.Root.SetActive(false);
        }

        private static IEnumerator BobRoutine(ItemSlotView slot)
        {
            const float amplitude = 0.04f;
            const float speed = 2.6f;
            while (slot.Root != null && slot.Root.activeSelf)
            {
                float bob = Mathf.Sin(Time.time * speed) * amplitude;
                slot.Root.transform.localPosition = slot.BasePosition + Vector3.up * bob;
                yield return null;
            }
        }

        private static IEnumerator ClaimEffectRoutine(ItemSlotView slot)
        {
            if (slot.BobRoutine != null)
            {
                StopCoroutine(slot.BobRoutine);
                slot.BobRoutine = null;
            }

            var root = slot.Root.transform;
            var startScale = root.localScale;
            var startColor = slot.Renderer.color;
            const float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                root.localScale = Vector3.Lerp(startScale, startScale * 1.35f, t);
                slot.Renderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            HideSlot(slot);
        }

        private static Sprite LoadVendorSprite()
        {
            var tex = Resources.Load<Texture2D>("UI/items_vendor");
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0f), SpritePpu);
        }

        private static Sprite LoadClothSprite()
        {
            var tex = Resources.Load<Texture2D>("UI/vendor_cloth");
            if (tex == null)
                return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), SpritePpu);
        }
    }
}
