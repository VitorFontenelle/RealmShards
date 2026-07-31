using System.Collections;
using RealmShards.Core;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Animated wardrobe in the hub lobby.
    /// Sheet layout: 4x2 grid — open sequence (0-4), glow loop (4-7).
    /// </summary>
    public sealed class WardrobePedestal : MonoBehaviour
    {
        private const int ClosedFrame = 0;
        private const int GlowLoopStart = 4;
        private const int GlowLoopEnd = 7;
        private static readonly float[] OpenFrameDurations = { 0.12f, 0.12f, 0.14f, 0.16f, 0.18f };

        private enum WardrobeState
        {
            IdleClosed,
            Opening,
            GlowActive
        }

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private BoxCollider2D _collider;
        private Coroutine _routine;
        private WardrobeState _state = WardrobeState.IdleClosed;

        public bool IsGlowActive => _state == WardrobeState.GlowActive;

        public static WardrobePedestal Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("Wardrobe");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.3f;
            return go.AddComponent<WardrobePedestal>();
        }

        private void Awake()
        {
            _frames = LoadFrames();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _frames.Length > 0 ? _frames[ClosedFrame] : Enemies.EnemySpriteLoader.CreatePlaceholder(new Color(0.25f, 0.15f, 0.35f), 64);
            _renderer.sortingLayerName = SortingLayers.EnvironmentFront;
            _renderer.sortingOrder = 11;

            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            _collider.size = new Vector2(1.5f, 2.1f);
        }

        public bool ContainsPoint(Vector2 worldPoint) => _collider != null && _collider.OverlapPoint(worldPoint);

        public void PlayOpen(System.Action onMenuReady)
        {
            if (_state == WardrobeState.GlowActive)
            {
                onMenuReady?.Invoke();
                return;
            }

            StopRoutine();
            _routine = StartCoroutine(OpenSequence(onMenuReady));
        }

        public void BeginIdleClosed()
        {
            StopRoutine();
            _state = WardrobeState.IdleClosed;
            if (_frames.Length > ClosedFrame)
                _renderer.sprite = _frames[ClosedFrame];
        }

        private IEnumerator OpenSequence(System.Action onMenuReady)
        {
            _state = WardrobeState.Opening;

            if (_frames.Length == 0)
            {
                _state = WardrobeState.GlowActive;
                onMenuReady?.Invoke();
                _routine = StartCoroutine(GlowLoop());
                yield break;
            }

            int lastOpenFrame = Mathf.Min(GlowLoopStart, _frames.Length - 1);
            for (int frame = 1; frame <= lastOpenFrame; frame++)
            {
                _renderer.sprite = _frames[frame];
                int durationIndex = frame - 1;
                float wait = durationIndex < OpenFrameDurations.Length
                    ? OpenFrameDurations[durationIndex]
                    : 0.16f;
                yield return new WaitForSeconds(wait);
            }

            _state = WardrobeState.GlowActive;
            onMenuReady?.Invoke();
            _routine = StartCoroutine(GlowLoop());
        }

        private IEnumerator GlowLoop()
        {
            if (_frames.Length <= GlowLoopStart)
                yield break;

            int frameIndex = GlowLoopStart;
            int lastFrame = Mathf.Min(GlowLoopEnd, _frames.Length - 1);
            while (_state == WardrobeState.GlowActive)
            {
                _renderer.sprite = _frames[frameIndex];
                frameIndex = frameIndex >= lastFrame ? GlowLoopStart : frameIndex + 1;
                yield return new WaitForSeconds(0.18f);
            }
        }

        private void StopRoutine()
        {
            if (_routine == null)
                return;

            StopCoroutine(_routine);
            _routine = null;
        }

        private static Sprite[] LoadFrames()
        {
            var tex = Resources.Load<Texture2D>("UI/lobby_wardrobe");
            if (tex == null)
                return System.Array.Empty<Sprite>();

            const int cols = 4;
            const int rows = 2;
            int frameW = tex.width / cols;
            int frameH = tex.height / rows;
            var frames = new Sprite[cols * rows];
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int y = tex.height - (row + 1) * frameH;
                    var rect = new Rect(col * frameW, y, frameW, frameH);
                    frames[index++] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.08f), 100f);
                }
            }

            return frames;
        }
    }
}
