using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Simple frame animator. Uses provided sprites when available; otherwise keeps tinted placeholder.
    /// 4-dir facing via flipX (8-dir deferred — sheet layout too messy).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemySpriteAnimator : MonoBehaviour
    {
        [SerializeField] private float fps = 8f;

        private SpriteRenderer _sr;
        private Sprite[] _walk;
        private Sprite[] _attack;
        private Sprite _fallback;
        private float _timer;
        private int _frame;
        private bool _attacking;
        private Color _baseColor = Color.white;

        public void Configure(Sprite[] walk, Sprite[] attack, float animFps, Color tint, Sprite fallback)
        {
            _sr = GetComponent<SpriteRenderer>();
            _walk = walk;
            _attack = attack;
            fps = Mathf.Max(1f, animFps);
            _baseColor = tint;
            _fallback = fallback;
            _sr.color = tint;

            if (_walk != null && _walk.Length > 0 && _walk[0] != null)
                _sr.sprite = _walk[0];
            else if (_fallback != null)
                _sr.sprite = _fallback;
        }

        public void SetAttacking(bool attacking)
        {
            if (_attacking == attacking)
                return;
            _attacking = attacking;
            _timer = 0f;
            _frame = 0;
        }

        public void Tick(Vector2 facing, bool moving)
        {
            if (_sr == null)
                _sr = GetComponent<SpriteRenderer>();

            if (Mathf.Abs(facing.x) > 0.05f)
                _sr.flipX = facing.x < 0f;

            var frames = _attacking ? _attack : _walk;
            if (frames == null || frames.Length == 0)
            {
                if (_sr.sprite == null && _fallback != null)
                    _sr.sprite = _fallback;
                _sr.color = _attacking ? Color.Lerp(_baseColor, Color.red, 0.35f) : _baseColor;
                return;
            }

            if (!_attacking && !moving)
            {
                _sr.sprite = frames[0];
                _sr.color = _baseColor;
                return;
            }

            _timer += Time.deltaTime;
            float step = 1f / fps;
            while (_timer >= step)
            {
                _timer -= step;
                _frame = (_frame + 1) % frames.Length;
            }

            if (frames[_frame] != null)
                _sr.sprite = frames[_frame];

            _sr.color = _attacking ? Color.Lerp(_baseColor, new Color(1f, 0.55f, 0.55f), 0.4f) : _baseColor;
        }
    }
}
