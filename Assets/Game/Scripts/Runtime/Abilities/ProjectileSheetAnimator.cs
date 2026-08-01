using System;
using System.Collections;
using UnityEngine;

namespace RealmShards
{
    /// <summary>
    /// Horizontal sprite-sheet animation for projectiles (flight loop + optional vanish frame).
    /// </summary>
    public sealed class ProjectileSheetAnimator : MonoBehaviour
    {
        [SerializeField] private string resourcesPath = "Spells/air_bullet";
        [SerializeField] private int frameCount = 4;
        [SerializeField] private int flightFrameCount = 3;
        [SerializeField] private float spritePpu = 100f;
        [SerializeField] private float flightFps = 14f;
        [SerializeField] private float vanishDuration = 0.12f;

        private SpriteRenderer _renderer;
        private Sprite[] _frames;
        private float _animTimer;
        private int _flightIndex;
        private bool _vanishing;
        private Coroutine _vanishRoutine;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            LoadFrames();
        }

        public void ResetFlight()
        {
            _vanishing = false;
            if (_vanishRoutine != null)
            {
                StopCoroutine(_vanishRoutine);
                _vanishRoutine = null;
            }

            _animTimer = 0f;
            _flightIndex = 0;
            ApplyFlightFrame(0);
        }

        public void TickFlight(float deltaTime)
        {
            if (_vanishing || _frames == null || _frames.Length == 0)
                return;

            int lastFlight = Mathf.Min(flightFrameCount, _frames.Length) - 1;
            if (lastFlight <= 0)
                return;

            _animTimer += deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, flightFps);
            while (_animTimer >= frameDuration)
            {
                _animTimer -= frameDuration;
                _flightIndex = _flightIndex >= lastFlight ? 0 : _flightIndex + 1;
                ApplyFlightFrame(_flightIndex);
            }
        }

        public bool PlayVanish(Action onComplete)
        {
            if (_vanishing)
                return false;

            LoadFrames();
            if (_frames == null || _frames.Length < frameCount)
            {
                onComplete?.Invoke();
                return false;
            }

            _vanishing = true;
            _vanishRoutine = StartCoroutine(VanishRoutine(onComplete));
            return true;
        }

        private IEnumerator VanishRoutine(Action onComplete)
        {
            int vanishIndex = Mathf.Clamp(frameCount - 1, 0, _frames.Length - 1);
            if (_renderer != null)
            {
                _renderer.sprite = _frames[vanishIndex];
                var startColor = _renderer.color;
                float elapsed = 0f;
                while (elapsed < vanishDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / vanishDuration);
                    _renderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                    yield return null;
                }
            }

            _vanishing = false;
            _vanishRoutine = null;
            onComplete?.Invoke();
        }

        private void ApplyFlightFrame(int index)
        {
            if (_renderer == null || _frames == null || _frames.Length == 0)
                return;

            int clamped = Mathf.Clamp(index, 0, Mathf.Min(flightFrameCount, _frames.Length) - 1);
            _renderer.sprite = _frames[clamped];
        }

        private void LoadFrames()
        {
            if (_frames != null && _frames.Length > 0)
                return;

            var tex = Resources.Load<Texture2D>(resourcesPath);
            if (tex == null || frameCount <= 0)
                return;

            int frameW = tex.width / frameCount;
            int frameH = tex.height;
            _frames = new Sprite[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var rect = new Rect(i * frameW, 0f, frameW, frameH);
                _frames[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), spritePpu);
            }
        }
    }
}
