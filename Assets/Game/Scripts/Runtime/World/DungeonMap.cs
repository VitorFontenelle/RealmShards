using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.World
{
    public enum DungeonCell : byte
    {
        Void = 0,
        Floor = 1,
        Corridor = 2
    }

    public sealed class DungeonRoomInfo
    {
        public int Index;
        public bool IsChampion;
        public RectInt Cells;
        public Vector2 CenterWorld;
        public readonly List<Vector3> EnemySpawns = new List<Vector3>(8);
        public Vector3 ChampionSpawn;
        public Vector3 PlayerSpawn;
    }

    /// <summary>
    /// Grid occupancy for a rooms-and-connectors dungeon. World origin is map center.
    /// </summary>
    public sealed class DungeonMap
    {
        public readonly int Width;
        public readonly int Height;
        public readonly float CellSize;
        public readonly Vector2 Origin;
        public readonly DungeonCell[] Cells;
        public readonly List<DungeonRoomInfo> Rooms = new List<DungeonRoomInfo>(8);

        public DungeonMap(int width, int height, float cellSize)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            Origin = new Vector2(-width * cellSize * 0.5f, -height * cellSize * 0.5f);
            Cells = new DungeonCell[width * height];
        }

        public int Index(int x, int y) => y * Width + x;

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public DungeonCell Get(int x, int y) => InBounds(x, y) ? Cells[Index(x, y)] : DungeonCell.Void;

        public void Set(int x, int y, DungeonCell cell)
        {
            if (InBounds(x, y))
                Cells[Index(x, y)] = cell;
        }

        public bool IsWalkable(int x, int y)
        {
            var c = Get(x, y);
            return c == DungeonCell.Floor || c == DungeonCell.Corridor;
        }

        public Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(
                Origin.x + (x + 0.5f) * CellSize,
                Origin.y + (y + 0.5f) * CellSize,
                0f);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            int x = Mathf.FloorToInt((world.x - Origin.x) / CellSize);
            int y = Mathf.FloorToInt((world.y - Origin.y) / CellSize);
            return new Vector2Int(x, y);
        }

        public Vector2 WorldSize => new Vector2(Width * CellSize, Height * CellSize);

        public DungeonRoomInfo FindRoomAt(Vector3 world)
        {
            var cell = WorldToCell(world);
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i].Cells.Contains(cell))
                    return Rooms[i];
            }

            return null;
        }
    }
}
