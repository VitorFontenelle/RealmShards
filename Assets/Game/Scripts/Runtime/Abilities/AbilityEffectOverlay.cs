using UnityEngine;

namespace RealmShards
{
    public sealed class AbilityEffectOverlay : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float defaultLifetime = 0.25f;

        private PrefabPool _pool;
        private Transform _follow;
        private float _timer;
        private bool _active;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_follow != null)
            {
                transform.position = _follow.position;
            }

            _timer -= Time.deltaTime;
            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = Mathf.Clamp01(_timer / Mathf.Max(0.01f, defaultLifetime));
                spriteRenderer.color = c;
            }

            if (_timer <= 0f)
            {
                Despawn();
            }
        }

        public void OnSpawned(PrefabPool pool)
        {
            _pool = pool;
        }

        public void OnDespawned()
        {
            _active = false;
            _follow = null;
        }

        public void Play(Transform follow, Vector2 aim, float lifetime, Color tint)
        {
            _follow = follow;
            defaultLifetime = Mathf.Max(0.05f, lifetime);
            _timer = defaultLifetime;
            _active = true;

            if (aim.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = Core.SortingLayers.SkillEffectsFront;
                if (spriteRenderer.sortingOrder < 20)
                    spriteRenderer.sortingOrder = 25;
                tint.a = 0.85f;
                spriteRenderer.color = tint;
            }

            if (follow != null)
            {
                transform.position = follow.position;
            }

            gameObject.SetActive(true);
        }

        private void Despawn()
        {
            _active = false;
            if (_pool != null)
            {
                _pool.Release(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
