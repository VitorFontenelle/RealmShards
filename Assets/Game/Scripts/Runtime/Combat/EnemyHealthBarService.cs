using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Combat
{
    /// <summary>
    /// Transient enemy health bars that appear on hit, follow the unit, then fade out.
    /// </summary>
    public sealed class EnemyHealthBarService : MonoBehaviour
    {
        private const float BarWidth = 0.72f;
        private const float BarHeight = 0.055f;
        private const float HeadOffset = 0.1f;
        private const float HoldSeconds = 2f;
        private const float FadeSeconds = 1.1f;

        private static EnemyHealthBarService _instance;
        private static Sprite _whiteSprite;

        private readonly Dictionary<Health, BarEntry> _bars = new Dictionary<Health, BarEntry>(32);
        private Transform _root;

        private sealed class BarEntry
        {
            public Health Target;
            public Transform Root;
            public SpriteRenderer Background;
            public Transform FillTransform;
            public SpriteRenderer Fill;
            public float FadeTimer;
            public bool Subscribed;
        }

        public static EnemyHealthBarService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<EnemyHealthBarService>();
                    if (_instance == null)
                    {
                        var go = new GameObject(nameof(EnemyHealthBarService));
                        _instance = go.AddComponent<EnemyHealthBarService>();
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _root = transform;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void LateUpdate()
        {
            if (!Core.SettingsService.DisplayHealthbarsEnabled)
            {
                HideAllBars();
                return;
            }

            var remove = (List<Health>)null;
            foreach (var pair in _bars)
            {
                var entry = pair.Value;
                if (entry.Target == null || !entry.Target.IsAlive)
                {
                    remove ??= new List<Health>();
                    remove.Add(pair.Key);
                    continue;
                }

                UpdateBarPosition(entry);
                UpdateBarFill(entry);
                UpdateBarFade(entry);
            }

            if (remove != null)
            {
                for (int i = 0; i < remove.Count; i++)
                    RemoveBar(remove[i]);
            }
        }

        public static void NotifyDamaged(Health health)
        {
            if (!Core.SettingsService.DisplayHealthbarsEnabled || health == null || !health.IsAlive)
                return;

            if (health.Faction != FactionId.Enemy)
                return;

            Instance.ShowOrRefresh(health);
        }

        private void ShowOrRefresh(Health health)
        {
            if (!_bars.TryGetValue(health, out var entry))
            {
                entry = CreateBar(health);
                _bars[health] = entry;
            }

            entry.FadeTimer = 0f;
            entry.Root.gameObject.SetActive(true);
            UpdateBarFill(entry);
            SetBarAlpha(entry, 1f);
        }

        private BarEntry CreateBar(Health health)
        {
            var entry = new BarEntry { Target = health, FadeTimer = 0f };

            var barGo = new GameObject($"HealthBar_{health.name}");
            barGo.transform.SetParent(_root, false);
            entry.Root = barGo.transform;

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(entry.Root, false);
            entry.Background = bgGo.AddComponent<SpriteRenderer>();
            entry.Background.sprite = GetWhiteSprite();
            entry.Background.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
            entry.Background.drawMode = SpriteDrawMode.Sliced;
            entry.Background.size = new Vector2(BarWidth, BarHeight);
            ConfigureRenderer(entry.Background, 54);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(entry.Root, false);
            entry.FillTransform = fillGo.transform;
            entry.FillTransform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
            entry.Fill = fillGo.AddComponent<SpriteRenderer>();
            entry.Fill.sprite = GetWhiteSprite();
            entry.Fill.drawMode = SpriteDrawMode.Sliced;
            entry.Fill.size = new Vector2(BarWidth, BarHeight * 0.72f);
            ConfigureRenderer(entry.Fill, 55);

            health.Died += OnTargetDied;
            entry.Subscribed = true;
            UpdateBarPosition(entry);
            return entry;
        }

        private void OnTargetDied(Health health)
        {
            RemoveBar(health);
        }

        private void RemoveBar(Health health)
        {
            if (!_bars.TryGetValue(health, out var entry))
                return;

            if (entry.Subscribed)
                health.Died -= OnTargetDied;

            if (entry.Root != null)
                Destroy(entry.Root.gameObject);

            _bars.Remove(health);
        }

        private void HideAllBars()
        {
            foreach (var pair in _bars)
            {
                if (pair.Value.Root != null)
                    pair.Value.Root.gameObject.SetActive(false);
            }
        }

        private static void UpdateBarPosition(BarEntry entry)
        {
            var target = entry.Target.transform;
            float topY = target.position.y + 0.55f;
            var renderers = target.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                    continue;
                topY = Mathf.Max(topY, renderers[i].bounds.max.y);
            }

            entry.Root.position = new Vector3(target.position.x, topY + HeadOffset, 0f);
        }

        private static void UpdateBarFill(BarEntry entry)
        {
            float ratio = Mathf.Clamp01(entry.Target.CurrentHealth / Mathf.Max(1f, entry.Target.MaxHealth));
            entry.Fill.size = new Vector2(BarWidth * ratio, BarHeight * 0.72f);
            entry.FillTransform.localPosition = new Vector3(-BarWidth * 0.5f, 0f, 0f);
            entry.Fill.color = GetHealthColor(ratio);
        }

        private void UpdateBarFade(BarEntry entry)
        {
            entry.FadeTimer += Time.deltaTime;
            if (entry.FadeTimer <= HoldSeconds)
            {
                SetBarAlpha(entry, 1f);
                return;
            }

            float fadeT = (entry.FadeTimer - HoldSeconds) / FadeSeconds;
            if (fadeT >= 1f)
            {
                entry.Root.gameObject.SetActive(false);
                return;
            }

            SetBarAlpha(entry, 1f - fadeT);
        }

        private static void SetBarAlpha(BarEntry entry, float alpha)
        {
            var bg = entry.Background.color;
            bg.a = 0.9f * alpha;
            entry.Background.color = bg;

            var fill = entry.Fill.color;
            fill.a = alpha;
            entry.Fill.color = fill;
        }

        private static Color GetHealthColor(float ratio)
        {
            if (ratio > 0.5f)
                return new Color(0.22f, 0.9f, 0.32f, 1f);
            if (ratio > 0.25f)
                return new Color(0.95f, 0.84f, 0.18f, 1f);
            return new Color(0.95f, 0.22f, 0.18f, 1f);
        }

        private static void ConfigureRenderer(SpriteRenderer renderer, int order)
        {
            renderer.sortingLayerName = Core.SortingLayers.SkillEffectsFront;
            renderer.sortingOrder = order;
        }

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }
    }
}
