using RealmShards.Core;
using RealmShards.Rooms;
using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Builds a rectangular arena: tiled floor, wall colliders, spawn points, exit blockers.
    /// </summary>
    public static class ArenaBuilder
    {
        public const string FloorTilePath = "Assets/Tiles/sample-tile.png";

        public struct ArenaResult
        {
            public Transform Root;
            public RoomBounds Bounds;
            public Transform PlayerSpawn;
            public System.Collections.Generic.List<SpawnPoint> EnemySpawns;
            public System.Collections.Generic.List<SpawnPoint> ChampionSpawns;
            public Transform[] ExitBlockers;
        }

        public static ArenaResult Build(Vector2 roomSize, Transform parent = null)
        {
            var root = new GameObject("Arena");
            if (parent != null)
                root.transform.SetParent(parent);
            root.transform.position = Vector3.zero;

            var boundsGo = new GameObject("RoomBounds");
            boundsGo.transform.SetParent(root.transform);
            var bounds = boundsGo.AddComponent<RoomBounds>();
            bounds.Configure(roomSize);

            BuildFloor(root.transform, roomSize);
            var blockers = BuildWalls(root.transform, roomSize);

            var playerSpawn = CreateSpawn(root.transform, SpawnPointKind.Player, new Vector3(0f, -roomSize.y * 0.25f, 0f), "PlayerSpawn");
            var enemySpawns = new System.Collections.Generic.List<SpawnPoint>();
            Vector3[] enemyOffsets =
            {
                new Vector3(-6f, 3f, 0f),
                new Vector3(6f, 3f, 0f),
                new Vector3(-4f, 5.5f, 0f),
                new Vector3(4f, 5.5f, 0f),
                new Vector3(0f, 6.5f, 0f),
                new Vector3(-7f, 0f, 0f),
                new Vector3(7f, 0f, 0f)
            };
            for (int i = 0; i < enemyOffsets.Length; i++)
                enemySpawns.Add(CreateSpawn(root.transform, SpawnPointKind.Enemy, enemyOffsets[i], $"EnemySpawn_{i}"));

            var champSpawns = new System.Collections.Generic.List<SpawnPoint>
            {
                CreateSpawn(root.transform, SpawnPointKind.Champion, new Vector3(0f, roomSize.y * 0.28f, 0f), "ChampionSpawn")
            };

            return new ArenaResult
            {
                Root = root.transform,
                Bounds = bounds,
                PlayerSpawn = playerSpawn.transform,
                EnemySpawns = enemySpawns,
                ChampionSpawns = champSpawns,
                ExitBlockers = blockers
            };
        }

        private static void BuildFloor(Transform parent, Vector2 roomSize)
        {
            var floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(parent);

            Sprite tile = null;
#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(FloorTilePath);
            if (assets != null)
            {
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is Sprite s)
                    {
                        tile = s;
                        break;
                    }
                }
            }
#endif
            if (tile == null)
                tile = Enemies.EnemySpriteLoader.CreatePlaceholder(new Color(0.35f, 0.32f, 0.28f), 64);

            float tileWorld = tile.bounds.size.x;
            if (tileWorld < 0.1f)
                tileWorld = 12.5f;

            float desired = 4f;
            float scale = desired / tileWorld;
            float step = desired;

            int cols = Mathf.CeilToInt(roomSize.x / step) + 1;
            int rows = Mathf.CeilToInt(roomSize.y / step) + 1;
            Vector3 origin = new Vector3(-roomSize.x * 0.5f, -roomSize.y * 0.5f, 0f);

            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                var go = new GameObject($"Floor_{x}_{y}");
                go.transform.SetParent(floorRoot.transform);
                go.transform.position = origin + new Vector3(x * step + step * 0.5f, y * step + step * 0.5f, 0.1f);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tile;
                sr.sortingLayerName = SortingLayers.Ground;
                sr.sortingOrder = -20;
                sr.color = new Color(0.85f, 0.82f, 0.75f, 1f);
            }
        }

        private static Transform[] BuildWalls(Transform parent, Vector2 roomSize)
        {
            float thickness = 1f;
            float halfW = roomSize.x * 0.5f;
            float halfH = roomSize.y * 0.5f;

            CreateWall(parent, "Wall_N", new Vector3(0f, halfH + thickness * 0.5f, 0f), new Vector2(roomSize.x + thickness * 2f, thickness));
            CreateWall(parent, "Wall_S", new Vector3(0f, -halfH - thickness * 0.5f, 0f), new Vector2(roomSize.x + thickness * 2f, thickness));
            CreateWall(parent, "Wall_W", new Vector3(-halfW - thickness * 0.5f, 0f, 0f), new Vector2(thickness, roomSize.y));
            CreateWall(parent, "Wall_E", new Vector3(halfW + thickness * 0.5f, 0f, 0f), new Vector2(thickness, roomSize.y));

            var northDoor = CreateWall(parent, "ExitBlock_N", new Vector3(0f, halfH - 0.2f, 0f), new Vector2(3.5f, 0.6f), new Color(0.6f, 0.15f, 0.15f, 0.85f));
            var southDoor = CreateWall(parent, "ExitBlock_S", new Vector3(0f, -halfH + 0.2f, 0f), new Vector2(3.5f, 0.6f), new Color(0.6f, 0.15f, 0.15f, 0.85f));
            return new[] { northDoor, southDoor };
        }

        private static Transform CreateWall(Transform parent, string name, Vector3 pos, Vector2 size, Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.layer = GameLayers.Environment;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Enemies.EnemySpriteLoader.CreatePlaceholder(color ?? new Color(0.25f, 0.22f, 0.2f), 16);
            sr.sortingLayerName = SortingLayers.EnvironmentFront;
            sr.sortingOrder = 5;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            go.AddComponent<BoxCollider2D>().size = Vector2.one;
            return go.transform;
        }

        private static SpawnPoint CreateSpawn(Transform parent, SpawnPointKind kind, Vector3 localPos, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = localPos;
            var sp = go.AddComponent<SpawnPoint>();
            sp.Configure(kind, name);
            return sp;
        }
    }
}
