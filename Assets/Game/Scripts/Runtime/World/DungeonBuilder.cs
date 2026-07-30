using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Rooms;
using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Turns a <see cref="DungeonMap"/> into floor sprites (walkable only), wall colliders on void edges, and spawns.
    /// Void stays black (camera clear color) — no tiles outside ground.
    /// </summary>
    public static class DungeonBuilder
    {
        public static ArenaBuilder.ArenaResult Build(int seed, int roomCount, Transform parent = null)
        {
            var map = DungeonGenerator.Generate(seed, roomCount);
            var root = new GameObject("Dungeon");
            if (parent != null)
                root.transform.SetParent(parent);
            root.transform.position = Vector3.zero;

            var mapHost = root.AddComponent<DungeonMapHost>();
            mapHost.Assign(map);

            var boundsGo = new GameObject("DungeonBounds");
            boundsGo.transform.SetParent(root.transform);
            var bounds = boundsGo.AddComponent<RoomBounds>();
            bounds.Configure(map.WorldSize + new Vector2(map.CellSize * 2f, map.CellSize * 2f));

            BuildFloor(root.transform, map);
            BuildWallColliders(root.transform, map);

            var enemySpawns = new List<SpawnPoint>();
            var champSpawns = new List<SpawnPoint>();
            Transform playerSpawn = null;

            for (int i = 0; i < map.Rooms.Count; i++)
            {
                var room = map.Rooms[i];
                if (i == 0)
                    playerSpawn = CreateSpawn(root.transform, SpawnPointKind.Player, room.PlayerSpawn, "PlayerSpawn").transform;

                for (int e = 0; e < room.EnemySpawns.Count; e++)
                    enemySpawns.Add(CreateSpawn(root.transform, SpawnPointKind.Enemy, room.EnemySpawns[e], $"EnemySpawn_R{i}_{e}"));

                if (room.IsChampion)
                    champSpawns.Add(CreateSpawn(root.transform, SpawnPointKind.Champion, room.ChampionSpawn, "ChampionSpawn"));
            }

            if (playerSpawn == null && map.Rooms.Count > 0)
                playerSpawn = CreateSpawn(root.transform, SpawnPointKind.Player, map.Rooms[0].PlayerSpawn, "PlayerSpawn").transform;

            var exploration = root.AddComponent<ExplorationFog>();
            exploration.Configure(map);

            return new ArenaBuilder.ArenaResult
            {
                Root = root.transform,
                Bounds = bounds,
                PlayerSpawn = playerSpawn,
                EnemySpawns = enemySpawns,
                ChampionSpawns = champSpawns,
                ExitBlockers = System.Array.Empty<Transform>(),
                Map = map,
                Exploration = exploration
            };
        }

        private static void BuildFloor(Transform parent, DungeonMap map)
        {
            var floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(parent);

            Sprite tile = ArenaBuilder.LoadFloorSpritePublic();
            if (tile == null)
                tile = Enemies.EnemySpriteLoader.CreatePlaceholder(new Color(0.4f, 0.36f, 0.3f), 64);

            float tileWorld = Mathf.Max(0.1f, tile.bounds.size.x);
            float scale = map.CellSize / tileWorld;

            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!map.IsWalkable(x, y))
                    continue;

                var go = new GameObject($"Floor_{x}_{y}");
                go.transform.SetParent(floorRoot.transform);
                go.transform.position = map.CellToWorld(x, y);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tile;
                sr.sortingLayerName = SortingLayers.Ground;
                sr.sortingOrder = map.Get(x, y) == DungeonCell.Corridor ? -21 : -20;
                sr.color = map.Get(x, y) == DungeonCell.Corridor
                    ? new Color(0.85f, 0.82f, 0.78f, 1f)
                    : Color.white;
            }
        }

        private static void BuildWallColliders(Transform parent, DungeonMap map)
        {
            var wallsRoot = new GameObject("Walls");
            wallsRoot.transform.SetParent(parent);

            // Collide on void cells that touch walkable ground — black void acts as wall.
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (map.IsWalkable(x, y))
                    continue;
                if (!TouchesWalkable(map, x, y))
                    continue;

                var go = new GameObject($"Wall_{x}_{y}");
                go.transform.SetParent(wallsRoot.transform);
                go.transform.position = map.CellToWorld(x, y);
                go.layer = GameLayers.Environment;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(map.CellSize, map.CellSize);
            }

            // Outer border ring beyond the grid.
            float pad = map.CellSize;
            float halfW = map.WorldSize.x * 0.5f + pad;
            float halfH = map.WorldSize.y * 0.5f + pad;
            CreateBorder(wallsRoot.transform, "Border_N", new Vector3(0f, halfH, 0f), new Vector2(map.WorldSize.x + pad * 2f, pad));
            CreateBorder(wallsRoot.transform, "Border_S", new Vector3(0f, -halfH, 0f), new Vector2(map.WorldSize.x + pad * 2f, pad));
            CreateBorder(wallsRoot.transform, "Border_W", new Vector3(-halfW, 0f, 0f), new Vector2(pad, map.WorldSize.y));
            CreateBorder(wallsRoot.transform, "Border_E", new Vector3(halfW, 0f, 0f), new Vector2(pad, map.WorldSize.y));
        }

        private static bool TouchesWalkable(DungeonMap map, int x, int y)
        {
            return map.IsWalkable(x + 1, y) || map.IsWalkable(x - 1, y) ||
                   map.IsWalkable(x, y + 1) || map.IsWalkable(x, y - 1);
        }

        private static void CreateBorder(Transform parent, string name, Vector3 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.layer = GameLayers.Environment;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static SpawnPoint CreateSpawn(Transform parent, SpawnPointKind kind, Vector3 worldPos, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = worldPos;
            var sp = go.AddComponent<SpawnPoint>();
            sp.Configure(kind, name);
            return sp;
        }
    }

    /// <summary>Holds generated map on the dungeon root for minimap / probes.</summary>
    public sealed class DungeonMapHost : MonoBehaviour
    {
        public DungeonMap Map { get; private set; }
        public void Assign(DungeonMap map) => Map = map;
    }
}
