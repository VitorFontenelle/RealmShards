using UnityEngine;

namespace RealmShards
{
    public enum FacingDirection8
    {
        South = 0,
        SouthEast = 1,
        East = 2,
        NorthEast = 3,
        North = 4,
        NorthWest = 5,
        West = 6,
        SouthWest = 7
    }

    public static class FacingUtility
    {
        public static FacingDirection8 FromVector(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
            {
                return FacingDirection8.South;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Convert so 0 = South, increasing counter-clockwise in 45° steps.
            // Atan2: 0 = East, 90 = North, ±180 = West, -90 = South
            float normalized = (90f - angle + 360f) % 360f;
            int index = Mathf.RoundToInt(normalized / 45f) % 8;
            return (FacingDirection8)index;
        }

        public static Vector2 ToVector(FacingDirection8 facing)
        {
            return facing switch
            {
                FacingDirection8.South => Vector2.down,
                FacingDirection8.SouthEast => new Vector2(1f, -1f).normalized,
                FacingDirection8.East => Vector2.right,
                FacingDirection8.NorthEast => new Vector2(1f, 1f).normalized,
                FacingDirection8.North => Vector2.up,
                FacingDirection8.NorthWest => new Vector2(-1f, 1f).normalized,
                FacingDirection8.West => Vector2.left,
                FacingDirection8.SouthWest => new Vector2(-1f, -1f).normalized,
                _ => Vector2.down
            };
        }
    }
}
