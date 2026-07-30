using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Places rectangular rooms then carves L-shaped connectors between them.
    /// Seeded so each run/city produces a different layout.
    /// </summary>
    public static class DungeonGenerator
    {
        public static DungeonMap Generate(int seed, int roomCount, float cellSize = 1.5f)
        {
            roomCount = Mathf.Clamp(roomCount, 2, 6);
            var rng = new System.Random(seed);

            int width = 40 + roomCount * 4;
            int height = 28 + roomCount * 3;
            var map = new DungeonMap(width, height, cellSize);

            var placed = new List<RectInt>(roomCount);
            int attempts = 0;
            while (placed.Count < roomCount && attempts < 200)
            {
                attempts++;
                int rw = rng.Next(7, 12);
                int rh = rng.Next(6, 10);
                int rx = rng.Next(2, width - rw - 2);
                int ry = rng.Next(2, height - rh - 2);
                var rect = new RectInt(rx, ry, rw, rh);

                bool overlaps = false;
                for (int i = 0; i < placed.Count; i++)
                {
                    var expanded = Expand(placed[i], 2);
                    if (expanded.Overlaps(Expand(rect, 1)))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                    continue;

                CarveRect(map, rect, DungeonCell.Floor);
                placed.Add(rect);
            }

            // Guarantee at least 2 rooms if RNG was unlucky.
            while (placed.Count < Mathf.Min(2, roomCount))
            {
                int i = placed.Count;
                var rect = new RectInt(3 + i * 12, 8, 8, 7);
                if (rect.xMax >= width - 1) rect.x = width - rect.width - 2;
                CarveRect(map, rect, DungeonCell.Floor);
                placed.Add(rect);
            }

            for (int i = 1; i < placed.Count; i++)
                CarveCorridor(map, CenterOf(placed[i - 1]), CenterOf(placed[i]), rng);

            // Occasional extra connector for loops.
            if (placed.Count >= 3 && rng.NextDouble() < 0.55)
                CarveCorridor(map, CenterOf(placed[0]), CenterOf(placed[placed.Count - 1]), rng);

            for (int i = 0; i < placed.Count; i++)
            {
                var rect = placed[i];
                var centerCell = CenterOf(rect);
                var room = new DungeonRoomInfo
                {
                    Index = i,
                    IsChampion = i == placed.Count - 1,
                    Cells = rect,
                    CenterWorld = map.CellToWorld(centerCell.x, centerCell.y),
                    PlayerSpawn = map.CellToWorld(centerCell.x, centerCell.y - Mathf.Max(1, rect.height / 4)),
                    ChampionSpawn = map.CellToWorld(centerCell.x, centerCell.y + Mathf.Max(1, rect.height / 5))
                };

                int enemyCount = room.IsChampion ? 2 : 3 + (i % 2);
                for (int e = 0; e < enemyCount; e++)
                {
                    float angle = (e / (float)enemyCount) * Mathf.PI * 2f + i;
                    int ex = centerCell.x + Mathf.RoundToInt(Mathf.Cos(angle) * (rect.width * 0.28f));
                    int ey = centerCell.y + Mathf.RoundToInt(Mathf.Sin(angle) * (rect.height * 0.28f));
                    ex = Mathf.Clamp(ex, rect.xMin + 1, rect.xMax - 2);
                    ey = Mathf.Clamp(ey, rect.yMin + 1, rect.yMax - 2);
                    room.EnemySpawns.Add(map.CellToWorld(ex, ey));
                }

                map.Rooms.Add(room);
            }

            return map;
        }

        private static RectInt Expand(RectInt r, int pad)
        {
            return new RectInt(r.xMin - pad, r.yMin - pad, r.width + pad * 2, r.height + pad * 2);
        }

        private static Vector2Int CenterOf(RectInt r)
        {
            return new Vector2Int(r.xMin + r.width / 2, r.yMin + r.height / 2);
        }

        private static void CarveRect(DungeonMap map, RectInt rect, DungeonCell cell)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
            for (int x = rect.xMin; x < rect.xMax; x++)
                map.Set(x, y, cell);
        }

        private static void CarveCorridor(DungeonMap map, Vector2Int a, Vector2Int b, System.Random rng)
        {
            int x = a.x;
            int y = a.y;
            bool horizontalFirst = rng.NextDouble() < 0.5;

            if (horizontalFirst)
            {
                while (x != b.x)
                {
                    PaintCorridor(map, x, y);
                    x += x < b.x ? 1 : -1;
                }
                while (y != b.y)
                {
                    PaintCorridor(map, x, y);
                    y += y < b.y ? 1 : -1;
                }
            }
            else
            {
                while (y != b.y)
                {
                    PaintCorridor(map, x, y);
                    y += y < b.y ? 1 : -1;
                }
                while (x != b.x)
                {
                    PaintCorridor(map, x, y);
                    x += x < b.x ? 1 : -1;
                }
            }

            PaintCorridor(map, b.x, b.y);
        }

        private static void PaintCorridor(DungeonMap map, int x, int y)
        {
            // 2-cell wide corridors for comfortable movement.
            for (int oy = 0; oy <= 1; oy++)
            for (int ox = 0; ox <= 1; ox++)
            {
                if (!map.InBounds(x + ox, y + oy))
                    continue;
                if (map.Get(x + ox, y + oy) == DungeonCell.Floor)
                    continue;
                map.Set(x + ox, y + oy, DungeonCell.Corridor);
            }
        }
    }
}
