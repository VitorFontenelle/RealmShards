using System.Collections;
using RealmShards.Core;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Animated spell tome pedestal in the hub lobby.
    /// Sheet layout: 4x2 grid — idle (0-1), open sequence (2-6), menu loop (7).
    /// </summary>
    public sealed class TomePedestal : MonoBehaviour
    {
        private const int IdleFrameA = 0;
        private const int IdleFrameB = 1;
        private const int OpenLoopFrame = 7;
        private const float IdleFrameDuration = 0.55f;
        private const float WorldScale = 0.58f;
        private const float SpritePpu = 200f;
        private static readonly float[] OpenFrameDurations = { 0.14f, 0.14f, 0.16f, 0.16f, 0.2f };

        private enum TomeState
        {
            IdleClosed,
            Opening,
            OpenActive
        }

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private BoxCollider2D _collider;
        private Coroutine _routine;
        private TomeState _state = TomeState.IdleClosed;

        public bool IsOpenActive => _state == TomeState.OpenActive;
        public Vector3 InteractPoint => transform.position + new Vector3(0f, 0.35f, 0f);

        public static TomePedestal Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("TomePedestal");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * WorldScale;
            go.layer = GameLayers.Environment;
            return go.AddComponent<TomePedestal>();
        }

        private void Awake()
        {
            _frames = LoadFrames();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _frames.Length > 0 ? _frames[IdleFrameA] : Enemies.EnemySpriteLoader.CreatePlaceholder(new Color(0.45f, 0.2f, 0.75f), 64);
            _renderer.sortingLayerName = SortingLayers.EnvironmentFront;
            _renderer.sortingOrder = 12;

            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = false;
            _collider.size = new Vector2(1.15f, 1.35f);
            _collider.offset = new Vector2(0f, 0.55f);

            BeginIdleClosed();
        }

        public bool ContainsPoint(Vector2 worldPoint) => _collider != null && _collider.OverlapPoint(worldPoint);

        public void PlayOpen(System.Action onMenuReady)
        {
            if (_state == TomeState.OpenActive)
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
            _state = TomeState.IdleClosed;
            if (_frames.Length > IdleFrameA)
                _renderer.sprite = _frames[IdleFrameA];
            _routine = StartCoroutine(IdleClosedLoop());
        }

        public void BeginOpenActive()
        {
            StopRoutine();
            _state = TomeState.OpenActive;
            if (_frames.Length > OpenLoopFrame)
                _renderer.sprite = _frames[OpenLoopFrame];
            _routine = StartCoroutine(OpenActiveLoop());
        }

        private IEnumerator IdleClosedLoop()
        {
            if (_frames.Length < 2)
                yield break;

            while (_state == TomeState.IdleClosed)
            {
                _renderer.sprite = _frames[IdleFrameA];
                yield return new WaitForSeconds(IdleFrameDuration);
                if (_state != TomeState.IdleClosed)
                    yield break;

                _renderer.sprite = _frames[IdleFrameB];
                yield return new WaitForSeconds(IdleFrameDuration);
            }
        }

        private IEnumerator OpenSequence(System.Action onMenuReady)
        {
            _state = TomeState.Opening;

            if (_frames.Length == 0)
            {
                _state = TomeState.OpenActive;
                onMenuReady?.Invoke();
                _routine = StartCoroutine(OpenActiveLoop());
                yield break;
            }

            for (int frame = 2; frame <= 6 && frame < _frames.Length; frame++)
            {
                _renderer.sprite = _frames[frame];
                int durationIndex = frame - 2;
                float wait = durationIndex < OpenFrameDurations.Length
                    ? OpenFrameDurations[durationIndex]
                    : 0.16f;
                yield return new WaitForSeconds(wait);
            }

            _state = TomeState.OpenActive;
            onMenuReady?.Invoke();
            _routine = StartCoroutine(OpenActiveLoop());
        }

        private IEnumerator OpenActiveLoop()
        {
            if (_frames.Length <= OpenLoopFrame)
                yield break;

            while (_state == TomeState.OpenActive)
            {
                _renderer.sprite = _frames[OpenLoopFrame];
                yield return new WaitForSeconds(0.45f);
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
            var tex = Resources.Load<Texture2D>("UI/tome_pedestal");
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
                    frames[index++] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.15f), SpritePpu);
                }
            }

            return frames;
        }
    }
}
