using System.Collections;
using RealmShards.Core;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Animated item chest in the hub lobby.
    /// Sheet layout: 4x2 grid — closed (0), open sequence (1-6), sparkle loop (6-7).
    /// </summary>
    public sealed class ItemChestPedestal : MonoBehaviour
    {
        private const int ClosedFrame = 0;
        private const int SparkleFrameA = 6;
        private const int SparkleFrameB = 7;
        private const float WorldScale = 0.55f;
        private const float SpritePpu = 200f;
        private static readonly float[] OpenFrameDurations = { 0.12f, 0.12f, 0.14f, 0.14f, 0.16f, 0.18f, 0.2f };

        private enum ChestState
        {
            IdleClosed,
            Opening,
            SparkleActive
        }

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private BoxCollider2D _collider;
        private Coroutine _routine;
        private ChestState _state = ChestState.IdleClosed;

        public bool IsSparkleActive => _state == ChestState.SparkleActive;
        public Vector3 InteractPoint => transform.position + new Vector3(0f, 0.25f, 0f);

        public static ItemChestPedestal Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("ItemChest");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * WorldScale;
            go.layer = GameLayers.Environment;
            return go.AddComponent<ItemChestPedestal>();
        }

        private void Awake()
        {
            _frames = LoadFrames();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _frames.Length > 0 ? _frames[ClosedFrame] : Enemies.EnemySpriteLoader.CreatePlaceholder(new Color(0.55f, 0.35f, 0.2f), 64);
            _renderer.sortingLayerName = SortingLayers.EnvironmentFront;
            _renderer.sortingOrder = 11;
            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = false;
            _collider.size = new Vector2(1.2f, 1.05f);
            _collider.offset = new Vector2(0f, 0.45f);
        }

        public bool ContainsPoint(Vector2 worldPoint) => _collider != null && _collider.OverlapPoint(worldPoint);

        public void PlayOpen(System.Action onMenuReady)
        {
            if (_state == ChestState.SparkleActive)
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
            _state = ChestState.IdleClosed;
            if (_frames.Length > ClosedFrame)
                _renderer.sprite = _frames[ClosedFrame];
        }

        private IEnumerator OpenSequence(System.Action onMenuReady)
        {
            _state = ChestState.Opening;

            if (_frames.Length == 0)
            {
                _state = ChestState.SparkleActive;
                onMenuReady?.Invoke();
                _routine = StartCoroutine(SparkleLoop());
                yield break;
            }

            int lastOpenFrame = Mathf.Min(SparkleFrameA, _frames.Length - 1);
            for (int frame = 1; frame <= lastOpenFrame; frame++)
            {
                _renderer.sprite = _frames[frame];
                int durationIndex = frame - 1;
                float wait = durationIndex < OpenFrameDurations.Length
                    ? OpenFrameDurations[durationIndex]
                    : 0.16f;
                yield return new WaitForSeconds(wait);
            }

            _state = ChestState.SparkleActive;
            onMenuReady?.Invoke();
            _routine = StartCoroutine(SparkleLoop());
        }

        private IEnumerator SparkleLoop()
        {
            if (_frames.Length <= SparkleFrameB)
                yield break;

            bool toggle = false;
            while (_state == ChestState.SparkleActive)
            {
                _renderer.sprite = _frames[toggle ? SparkleFrameB : SparkleFrameA];
                toggle = !toggle;
                yield return new WaitForSeconds(0.22f);
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
            var tex = Resources.Load<Texture2D>("UI/item_chest");
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
                    frames[index++] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.12f), SpritePpu);
                }
            }

            return frames;
        }
    }
}
