using UnityEngine;

namespace RealmShards
{
    public enum AnimState
    {
        Idle,
        Run,
        Cast
    }

    public sealed class DirectionalSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private DirectionalAnimationSet animationSet;
        [SerializeField] private FacingDirection8 facing = FacingDirection8.South;

        private AnimState _state = AnimState.Idle;
        private float _frameTimer;
        private int _frameIndex;
        private float _castTimer;

        public FacingDirection8 Facing => facing;
        public AnimState State => _state;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null || animationSet == null)
            {
                return;
            }

            if (_state == AnimState.Cast)
            {
                _castTimer -= Time.deltaTime;
                float fps = animationSet.CastFps;
                _frameTimer += Time.deltaTime;
                if (_frameTimer >= 1f / fps)
                {
                    _frameTimer = 0f;
                    _frameIndex++;
                }

                spriteRenderer.sprite = animationSet.GetCastFrame(facing, _frameIndex);
                if (_castTimer <= 0f)
                {
                    _state = AnimState.Idle;
                    _frameIndex = 0;
                }

                return;
            }

            if (_state == AnimState.Run)
            {
                float fps = animationSet.RunFps;
                _frameTimer += Time.deltaTime;
                if (_frameTimer >= 1f / fps)
                {
                    _frameTimer = 0f;
                    _frameIndex = (_frameIndex + 1) % animationSet.FramesPerDirection;
                }

                spriteRenderer.sprite = animationSet.GetRunFrame(facing, _frameIndex);
                return;
            }

            spriteRenderer.sprite = animationSet.GetIdle(facing);
        }

        public void SetAnimationSet(DirectionalAnimationSet set)
        {
            animationSet = set;
        }

        public void SetFacing(FacingDirection8 newFacing)
        {
            facing = newFacing;
        }

        public void SetFacingFromVector(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            facing = FacingUtility.FromVector(direction);
        }

        public void SetMoving(bool moving)
        {
            if (_state == AnimState.Cast)
            {
                return;
            }

            AnimState next = moving ? AnimState.Run : AnimState.Idle;
            if (next == _state)
            {
                return;
            }

            _state = next;
            _frameIndex = 0;
            _frameTimer = 0f;
        }

        public void PlayCast(float duration = 0.35f)
        {
            _state = AnimState.Cast;
            _frameIndex = 0;
            _frameTimer = 0f;
            _castTimer = Mathf.Max(0.1f, duration);
        }
    }
}
