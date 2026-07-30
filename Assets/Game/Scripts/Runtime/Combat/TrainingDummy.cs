using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Simple damageable dummy so combat can be tested without enemy systems.
    /// </summary>
    public sealed class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 80f;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color healthyColor = new Color(0.85f, 0.35f, 0.35f);
        [SerializeField] private Color hurtColor = new Color(1f, 0.85f, 0.85f);

        private Health _health;
        private float _flashTimer;

        private void Awake()
        {
            CombatLayers.TrySetLayer(gameObject, CombatLayers.Enemy);

            if (GetComponent<FactionMember>() == null)
            {
                var faction = gameObject.AddComponent<FactionMember>();
                faction.Configure(FactionId.Enemy, 0);
            }

            _health = GetComponent<Health>();
            if (_health == null)
            {
                _health = gameObject.AddComponent<Health>();
            }

            _health.Configure(maxHealth, 0.1f);

            if (GetComponent<Hurtbox>() == null)
            {
                var hurtGo = new GameObject("Hurtbox");
                hurtGo.transform.SetParent(transform, false);
                var col = hurtGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;
                CombatLayers.TrySetLayer(hurtGo, CombatLayers.Enemy);
                hurtGo.AddComponent<Hurtbox>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = healthyColor;
            }

            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
        }

        private void Update()
        {
            if (_flashTimer > 0f && spriteRenderer != null)
            {
                _flashTimer -= Time.deltaTime;
                spriteRenderer.color = Color.Lerp(healthyColor, hurtColor, _flashTimer / 0.15f);
            }
        }

        private void OnDamaged(Health health, DamageInfo info)
        {
            _flashTimer = 0.15f;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtColor;
            }
        }

        private void OnDied(Health health)
        {
            if (spriteRenderer != null)
            {
                var c = healthyColor;
                c.a = 0.35f;
                spriteRenderer.color = c;
            }

            Destroy(gameObject, 0.75f);
        }
    }
}
