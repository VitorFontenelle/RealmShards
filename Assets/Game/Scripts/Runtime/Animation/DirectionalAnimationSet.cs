using UnityEngine;

namespace RealmShards
{
    [CreateAssetMenu(menuName = "RealmShards/Animation/Directional Animation Set", fileName = "DirectionalAnimationSet")]
    public sealed class DirectionalAnimationSet : ScriptableObject
    {
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] runFrames = new Sprite[48];
        [SerializeField] private Sprite[] castFrames = new Sprite[48];
        [SerializeField] private int framesPerDirection = 6;
        [SerializeField] private float runFps = 10f;
        [SerializeField] private float castFps = 12f;

        public Sprite IdleSprite => idleSprite;
        public Sprite[] RunFrames => runFrames;
        public Sprite[] CastFrames => castFrames;
        public int FramesPerDirection => Mathf.Max(1, framesPerDirection);
        public float RunFps => Mathf.Max(1f, runFps);
        public float CastFps => Mathf.Max(1f, castFps);

        public Sprite GetRunFrame(FacingDirection8 facing, int frame)
        {
            return GetFrame(runFrames, facing, frame);
        }

        public Sprite GetCastFrame(FacingDirection8 facing, int frame)
        {
            return GetFrame(castFrames, facing, frame);
        }

        public Sprite GetIdle(FacingDirection8 facing)
        {
            if (idleSprite != null)
            {
                return idleSprite;
            }

            return GetRunFrame(facing, 0);
        }

        private Sprite GetFrame(Sprite[] source, FacingDirection8 facing, int frame)
        {
            if (source == null || source.Length == 0)
            {
                return idleSprite;
            }

            int perDir = FramesPerDirection;
            int dirIndex = (int)facing;
            int index = dirIndex * perDir + (frame % perDir);
            if (index < 0 || index >= source.Length)
            {
                index = Mathf.Clamp(index, 0, source.Length - 1);
            }

            return source[index] != null ? source[index] : idleSprite;
        }

        public void SetSprites(Sprite idle, Sprite[] run, Sprite[] cast)
        {
            idleSprite = idle;
            runFrames = run;
            castFrames = cast;
        }
    }
}
