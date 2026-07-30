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
            // Magus sheets are authored North-first: N, NE, E, SE, S, SW, W, NW.
            int dirIndex = ToSheetRow(facing);
            int index = dirIndex * perDir + (frame % perDir);
            if (index < 0 || index >= source.Length)
            {
                index = Mathf.Clamp(index, 0, source.Length - 1);
            }

            return source[index] != null ? source[index] : idleSprite;
        }

        /// <summary>Maps runtime facing enum to Magus spritesheet row order.</summary>
        public static int ToSheetRow(FacingDirection8 facing)
        {
            return facing switch
            {
                FacingDirection8.North => 0,
                FacingDirection8.NorthEast => 1,
                FacingDirection8.East => 2,
                FacingDirection8.SouthEast => 3,
                FacingDirection8.South => 4,
                FacingDirection8.SouthWest => 5,
                FacingDirection8.West => 6,
                FacingDirection8.NorthWest => 7,
                _ => 4
            };
        }

        public void SetSprites(Sprite idle, Sprite[] run, Sprite[] cast)
        {
            idleSprite = idle;
            runFrames = run;
            castFrames = cast;
        }
    }
}
