using System.Collections;
using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Progression;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Animated spell tome pedestal in the hub lobby.
    /// </summary>
    public sealed class TomePedestal : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private BoxCollider2D _collider;
        private Coroutine _anim;

        public static TomePedestal Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("TomePedestal");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 1.35f;
            return go.AddComponent<TomePedestal>();
        }

        private void Awake()
        {
            _frames = LoadFrames();
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = _frames.Length > 0 ? _frames[0] : Enemies.EnemySpriteLoader.CreatePlaceholder(new Color(0.45f, 0.2f, 0.75f), 64);
            _renderer.sortingLayerName = SortingLayers.EnvironmentFront;
            _renderer.sortingOrder = 12;

            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.isTrigger = true;
            _collider.size = new Vector2(1.4f, 1.6f);
        }

        public bool ContainsPoint(Vector2 worldPoint) => _collider != null && _collider.OverlapPoint(worldPoint);

        public void PlayOpen(System.Action onMenuReady)
        {
            if (_anim != null)
                StopCoroutine(_anim);
            _anim = StartCoroutine(OpenSequence(onMenuReady));
        }

        public void SetIdleFrame()
        {
            if (_frames.Length > 0)
                _renderer.sprite = _frames[0];
        }

        public void SetOpenFrame()
        {
            if (_frames.Length > 0)
                _renderer.sprite = _frames[Mathf.Min(7, _frames.Length - 1)];
        }

        private IEnumerator OpenSequence(System.Action onMenuReady)
        {
            if (_frames.Length == 0)
            {
                onMenuReady?.Invoke();
                yield break;
            }

            for (int i = 0; i < _frames.Length; i++)
            {
                _renderer.sprite = _frames[i];
                float wait = i < 2 ? 0.12f : i < 5 ? 0.14f : 0.18f;
                yield return new WaitForSeconds(wait);
            }

            onMenuReady?.Invoke();
        }

        private static Sprite[] LoadFrames()
        {
            var tex = Resources.Load<Texture2D>("UI/tome_pedestal");
            if (tex == null)
                return System.Array.Empty<Sprite>();

            int cols = 4;
            int rows = 2;
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
                    frames[index++] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.15f), 100f);
                }
            }

            return frames;
        }
    }
}
