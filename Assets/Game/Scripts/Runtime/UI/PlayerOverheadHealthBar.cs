using RealmShards.Core;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Thin overhead health bar that follows a player in the hub lobby.
    /// </summary>
    public sealed class PlayerOverheadHealthBar : MonoBehaviour
    {
        private static Sprite _whiteSprite;
        private Transform _target;
        private Health _health;
        private Transform _root;
        private SpriteRenderer _fill;
        private SpriteRenderer _background;
        private const float BarWidth = 0.62f;
        private const float BarHeight = 0.05f;
        private const float HeadOffset = 0.18f;

        public static PlayerOverheadHealthBar Attach(Transform target, Health health)
        {
            var go = new GameObject("PlayerHealthBar");
            var bar = go.AddComponent<PlayerOverheadHealthBar>();
            bar.Initialize(target, health);
            return bar;
        }

        private void Initialize(Transform target, Health health)
        {
            _target = target;
            _health = health;
            _root = transform;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(_root, false);
            _background = bgGo.AddComponent<SpriteRenderer>();
            _background.sprite = GetWhiteSprite();
            _background.drawMode = SpriteDrawMode.Sliced;
            _background.size = new Vector2(BarWidth, BarHeight);
            _background.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
            ConfigureRenderer(_background, 40);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_root, false);
            fillGo.transform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
            _fill = fillGo.AddComponent<SpriteRenderer>();
            _fill.sprite = GetWhiteSprite();
            _fill.drawMode = SpriteDrawMode.Sliced;
            _fill.size = new Vector2(BarWidth, BarHeight * 0.72f);
            ConfigureRenderer(_fill, 41);
            RefreshFill();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            float topY = _target.position.y + 0.55f;
            var renderers = _target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled) continue;
                topY = Mathf.Max(topY, renderers[i].bounds.max.y);
            }

            _root.position = new Vector3(_target.position.x, topY + HeadOffset, 0f);
            RefreshFill();
        }

        private void RefreshFill()
        {
            if (_health == null || _fill == null) return;
            float ratio = Mathf.Clamp01(_health.CurrentHealth / Mathf.Max(1f, _health.MaxHealth));
            _fill.size = new Vector2(BarWidth * ratio, BarHeight * 0.72f);
            _fill.color = ratio > 0.5f
                ? new Color(0.22f, 0.9f, 0.32f, 1f)
                : ratio > 0.25f
                    ? new Color(0.95f, 0.84f, 0.18f, 1f)
                    : new Color(0.95f, 0.22f, 0.18f, 1f);
        }

        private static void ConfigureRenderer(SpriteRenderer renderer, int order)
        {
            renderer.sortingLayerName = SortingLayers.SkillEffectsFront;
            renderer.sortingOrder = order;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }
    }
}
