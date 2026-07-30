using System;
using UnityEngine;

namespace RealmShards.Rooms
{
    /// <summary>
    /// Deterministic per-city room count: 2–3 trash rooms then a champion room.
    /// </summary>
    public static class CityRoomPlanner
    {
        public const int MinTrashRooms = 2;
        public const int MaxTrashRooms = 3;

        public readonly struct Plan
        {
            public readonly int TrashRoomCount;
            public readonly int TotalRooms;
            public readonly int ChampionRoomIndex;

            public Plan(int trashRoomCount)
            {
                TrashRoomCount = Mathf.Clamp(trashRoomCount, MinTrashRooms, MaxTrashRooms);
                TotalRooms = TrashRoomCount + 1;
                ChampionRoomIndex = TrashRoomCount;
            }

            public bool IsChampionRoom(int roomIndex) => roomIndex >= ChampionRoomIndex;
            public bool IsFinalRoom(int roomIndex) => roomIndex >= ChampionRoomIndex;
        }

        public static Plan Build(int seed, int worldNodeIndex, bool isCapital)
        {
            // Capital: slightly shorter — 2 trash + champion.
            if (isCapital)
                return new Plan(MinTrashRooms);

            unchecked
            {
                int mixed = seed ^ (worldNodeIndex * 7919) ^ 0x5F3759DF;
                var rng = new System.Random(mixed);
                int trash = rng.Next(MinTrashRooms, MaxTrashRooms + 1);
                return new Plan(trash);
            }
        }
    }
}
