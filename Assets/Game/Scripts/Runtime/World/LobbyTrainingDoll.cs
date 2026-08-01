using System.Collections;
using RealmShards.Core;
using RealmShards.Enemies;
using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Lobby wooden training doll — reacts to spell hits with a tilt animation.
    /// </summary>
    public sealed class LobbyTrainingDoll : MonoBehaviour
    {
        private const float WorldScale = 0.55f;
        private const float SpritePpu = 200f;
        private const int FrameCount = 3;
        private static readonly float[] FrameDurations = { 0.07f, 0.07f, 0.08f, 0.08f, 0.07f, 0.07f };

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private Health _health;
        private Coroutine _hitRoutine;

        public static LobbyTrainingDoll Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("LobbyTrainingDoll");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * WorldScale;
            return go.AddComponent<LobbyTrainingDoll>();
        }

        private void Awake()
        {
            BuildVisuals();
            BuildCombat();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;
        }

        private void BuildVisuals()
        {
            _frames = LoadFrames();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _frames.Length > 0 ? _frames[0] : EnemySpriteLoader.CreatePlaceholder(new Color(0.55f, 0.4f, 0.28f), 64);
            _renderer.sortingLayerName = SortingLayers.EnvironmentFront;
            _renderer.sortingOrder = 10;
        }

        private void BuildCombat()
        {
            CombatLayers.TrySetLayer(gameObject, CombatLayers.Enemy);

            var faction = gameObject.AddComponent<FactionMember>();
            faction.Configure(FactionId.Enemy, 0);

            _health = gameObject.AddComponent<Health>();
            _health.Configure(10000f, 0f);
            _health.Damaged += OnDamaged;

            var hurtGo = new GameObject("Hurtbox");
            hurtGo.transform.SetParent(transform, false);
            hurtGo.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            CombatLayers.TrySetLayer(hurtGo, CombatLayers.Enemy);

            var col = hurtGo.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 1.35f);
            col.offset = new Vector2(0f, 0.1f);
            hurtGo.AddComponent<Hurtbox>();
        }

        private void OnDamaged(Health health, DamageInfo info)
        {
            _health.FullHeal();
            PlayHitReaction();
        }

        private void PlayHitReaction()
        {
            if (_hitRoutine != null)
                StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(HitReactionRoutine());
        }

        private IEnumerator HitReactionRoutine()
        {
            if (_frames.Length == 0)
                yield break;

            int peak = Mathf.Min(FrameCount - 1, _frames.Length - 1);

            for (int frame = 0; frame <= peak; frame++)
            {
                _renderer.sprite = _frames[frame];
                yield return new WaitForSeconds(FrameDuration(frame));
            }

            for (int frame = peak - 1; frame >= 0; frame--)
            {
                _renderer.sprite = _frames[frame];
                yield return new WaitForSeconds(FrameDuration(peak + (peak - frame)));
            }

            _renderer.sprite = _frames[0];
            _hitRoutine = null;
        }

        private static float FrameDuration(int index)
        {
            if (index < 0)
                return 0.07f;
            return index < FrameDurations.Length ? FrameDurations[index] : 0.07f;
        }

        private static Sprite[] LoadFrames()
        {
            var tex = Resources.Load<Texture2D>("UI/lobby_training_doll");
            if (tex == null)
                return System.Array.Empty<Sprite>();

            int frameW = tex.width / FrameCount;
            int frameH = tex.height;
            var frames = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                var rect = new Rect(i * frameW, 0f, frameW, frameH);
                frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.08f), SpritePpu);
            }

            return frames;
        }
    }
}
