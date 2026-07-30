using RealmShards.Core;
using RealmShards.Save;
using RealmShards.UI;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Revealed after champion defeat — opens Arcane Core unlock spend UI.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class ArcaneCoreTrigger : MonoBehaviour
    {
        [SerializeField] private string coreId = "arcane-core";
        [SerializeField] private bool consumeOnTouch = true;

        private bool _used;

        public static ArcaneCoreTrigger SpawnStub(Vector3 position)
        {
            var go = new GameObject("ArcaneCore");
            go.transform.position = position;
            go.layer = GameLayers.Trigger;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnemySpriteLoader.CreatePlaceholder(new Color(0.35f, 0.85f, 1f), 48);
            sr.sortingLayerName = SortingLayers.WorldUI;
            sr.sortingOrder = 15;
            sr.color = new Color(0.4f, 0.9f, 1f, 0.95f);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            go.AddComponent<ArcaneCorePulse>();
            return go.AddComponent<ArcaneCoreTrigger>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || other == null)
                return;

            bool isPlayer =
                other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerIdentity>() != null;
            if (!isPlayer)
                return;

            _used = true;
            OpenUnlockUi();
            if (consumeOnTouch)
                Destroy(gameObject, 0.2f);
        }

        private void OpenUnlockUi()
        {
            var cityId = GameContext.Instance?.RunSession?.CityId ?? ContentIdDefaults.CityStarter;
            string[] abilities;
            int[] costs;

            switch (cityId)
            {
                case ContentIdDefaults.CityGildedWard:
                    abilities = new[] { ContentIdDefaults.AbilityGildedFlare, ContentIdDefaults.AbilityGildedSmite };
                    costs = new[] { 18, 22 };
                    break;
                case ContentIdDefaults.CityAshenQuay:
                    abilities = new[] { ContentIdDefaults.AbilityAshenDrift, ContentIdDefaults.AbilityAshenCinder };
                    costs = new[] { 16, 18 };
                    break;
                case ContentIdDefaults.CityCapital:
                    abilities = new[] { ContentIdDefaults.AbilityContinuumSlip, ContentIdDefaults.AbilityContinuumEcho };
                    costs = new[] { 25, 28 };
                    break;
                default:
                    abilities = new[]
                    {
                        ContentIdDefaults.AbilityArcanePulse,
                        ContentIdDefaults.AbilityBlinkStep,
                        ContentIdDefaults.AbilityTideglassRipple,
                        ContentIdDefaults.AbilityTideglassHarpoon
                    };
                    costs = new[] { 15, 20, 16, 18 };
                    break;
            }

            var session = GameContext.Instance?.RunSession;
            if (session != null)
                session.AwaitingArcaneCore = true;

            ArcaneCoreUnlockScreen.Show(abilities, costs, () =>
            {
                if (session != null)
                    session.AwaitingArcaneCore = false;
                Debug.Log($"[RealmShards] Arcane Core '{coreId}' spend UI closed.");
            });
        }
    }

    public sealed class ArcaneCorePulse : MonoBehaviour
    {
        private Vector3 _baseScale;
        private void Awake() => _baseScale = transform.localScale;
        private void Update()
        {
            float s = 1f + Mathf.Sin(Time.time * 3f) * 0.12f;
            transform.localScale = _baseScale * s;
        }
    }
}
