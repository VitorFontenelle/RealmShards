using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Enemies;
using RealmShards.Rooms;
using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Cross-shaped hub lobby: central hub, four corridors, and side rooms.
    /// </summary>
    public static class HubLobbyArena
    {
        private const float TileStep = 4f;
        private const float WallThickness = 1f;
        private static readonly Color WallColor = new Color(0.25f, 0.22f, 0.2f);

        public struct LobbyArenaResult
        {
            public Transform Root;
            public RoomBounds Bounds;
            public Transform ExitTrigger;
            public Transform TomeSpawn;
            public Transform ChestSpawn;
            public Transform WardrobeSpawn;
            public Transform VendorSpawn;
            public Vector3[] TrainingDollSpawns;
            public Vector3[] PlayerSpawns;
        }

        public static LobbyArenaResult Build(Transform parent = null)
        {
            var rootGo = new GameObject("HubLobbyArena");
            if (parent != null)
                rootGo.transform.SetParent(parent, false);
            rootGo.transform.position = Vector3.zero;

            var floorRects = BuildFloorRects();
            var bounds = ComputeBounds(floorRects);

            var boundsGo = new GameObject("RoomBounds");
            boundsGo.transform.SetParent(rootGo.transform, false);
            var roomBounds = boundsGo.AddComponent<RoomBounds>();
            roomBounds.Configure(new Vector2(bounds.width, bounds.height), new Vector2(bounds.center.x, bounds.center.y));

            BuildFloor(rootGo.transform, floorRects);
            BuildPerimeterWalls(rootGo.transform, floorRects, bounds);

            var exitGo = CreateExit(rootGo.transform, new Vector3(0f, 18.5f, 0f));

            var tomeGo = CreateMarker(rootGo.transform, "TomeSpawn", new Vector3(19f, 0f, 0f));
            var chestGo = CreateMarker(rootGo.transform, "ChestSpawn", new Vector3(4f, -14.5f, 0f));
            var wardrobeGo = CreateMarker(rootGo.transform, "WardrobeSpawn", new Vector3(-19f, 3.2f, 0f));
            var vendorGo = CreateMarker(rootGo.transform, "VendorSpawn", new Vector3(-4f, -14.5f, 0f));

            var dollSpawns = new[]
            {
                new Vector3(-4f, 0f, 0f),
                new Vector3(4.5f, 2.4f, 0f),
                new Vector3(4.5f, 0f, 0f),
                new Vector3(4.5f, -2.4f, 0f)
            };

            var playerSpawns = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1.6f, 0.8f, 0f),
                new Vector3(-1.6f, 0.8f, 0f),
                new Vector3(0f, -1.2f, 0f)
            };

            return new LobbyArenaResult
            {
                Root = rootGo.transform,
                Bounds = roomBounds,
                ExitTrigger = exitGo,
                TomeSpawn = tomeGo,
                ChestSpawn = chestGo,
                WardrobeSpawn = wardrobeGo,
                VendorSpawn = vendorGo,
                TrainingDollSpawns = dollSpawns,
                PlayerSpawns = playerSpawns
            };
        }

        private static List<Rect> BuildFloorRects()
        {
            return new List<Rect>
            {
                // Central hub
                RectFromEdges(-8f, -7f, 8f, 7f),
                // Corridors
                RectFromEdges(-2f, 7f, 2f, 12f),
                RectFromEdges(-2f, -12f, 2f, -7f),
                RectFromEdges(8f, -2f, 13f, 2f),
                RectFromEdges(-13f, -2f, -8f, 2f),
                // Side rooms
                RectFromEdges(-6f, 12f, 6f, 20f),
                RectFromEdges(-7f, -17f, 7f, -12f),
                RectFromEdges(13f, -5f, 25f, 5f),
                RectFromEdges(-25f, -5f, -13f, 5f)
            };
        }

        private static Rect RectFromEdges(float minX, float minY, float maxX, float maxY) =>
            Rect.MinMaxRect(minX, minY, maxX, maxY);

        private static Rect ComputeBounds(List<Rect> rects)
        {
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            for (int i = 0; i < rects.Count; i++)
            {
                minX = Mathf.Min(minX, rects[i].xMin);
                minY = Mathf.Min(minY, rects[i].yMin);
                maxX = Mathf.Max(maxX, rects[i].xMax);
                maxY = Mathf.Max(maxY, rects[i].yMax);
            }

            const float pad = 1f;
            return Rect.MinMaxRect(minX - pad, minY - pad, maxX + pad, maxY + pad);
        }

        private static void BuildFloor(Transform parent, List<Rect> rects)
        {
            var floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(parent, false);

            Sprite tile = ArenaBuilder.LoadFloorSpritePublic();
            if (tile == null)
                tile = EnemySpriteLoader.CreatePlaceholder(new Color(0.35f, 0.32f, 0.28f), 64);

            float tileWorld = tile.bounds.size.x;
            if (tileWorld < 0.1f)
                tileWorld = 12.5f;

            float desired = TileStep;
            float scale = desired / tileWorld;

            var placed = new HashSet<long>();
            for (int r = 0; r < rects.Count; r++)
                StampFloorTiles(floorRoot.transform, tile, scale, rects[r], placed);
        }

        private static void StampFloorTiles(Transform parent, Sprite tile, float scale, Rect area, HashSet<long> placed)
        {
            int startX = Mathf.FloorToInt(area.xMin / TileStep);
            int endX = Mathf.CeilToInt(area.xMax / TileStep);
            int startY = Mathf.FloorToInt(area.yMin / TileStep);
            int endY = Mathf.CeilToInt(area.yMax / TileStep);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    float cx = x * TileStep + TileStep * 0.5f;
                    float cy = y * TileStep + TileStep * 0.5f;
                    if (!area.Contains(new Vector2(cx, cy)))
                        continue;

                    long key = ((long)x << 32) ^ (uint)y;
                    if (!placed.Add(key))
                        continue;

                    var go = new GameObject($"Floor_{x}_{y}");
                    go.transform.SetParent(parent, false);
                    go.transform.position = new Vector3(cx, cy, 0.1f);
                    go.transform.localScale = new Vector3(scale, scale, 1f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = tile;
                    sr.sortingLayerName = SortingLayers.Ground;
                    sr.sortingOrder = -20;
                    sr.color = Color.white;
                }
            }
        }

        private static void BuildPerimeterWalls(Transform parent, List<Rect> rects, Rect bounds)
        {
            var wallsRoot = new GameObject("Walls");
            wallsRoot.transform.SetParent(parent, false);

            float minX = bounds.xMin;
            float minY = bounds.yMin;
            int cols = Mathf.CeilToInt(bounds.width / TileStep) + 2;
            int rows = Mathf.CeilToInt(bounds.height / TileStep) + 2;

            var floor = new bool[cols, rows];
            for (int r = 0; r < rects.Count; r++)
                MarkFloorCells(floor, rects[r], minX, minY, cols, rows);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (!floor[x, y])
                        continue;

                    float cellMinX = minX + x * TileStep;
                    float cellMinY = minY + y * TileStep;

                    if (x == 0 || !floor[x - 1, y])
                        CreateWall(wallsRoot.transform, "Wall",
                            new Vector3(cellMinX - WallThickness * 0.5f, cellMinY + TileStep * 0.5f, 0f),
                            new Vector2(WallThickness, TileStep));

                    if (x == cols - 1 || !floor[x + 1, y])
                        CreateWall(wallsRoot.transform, "Wall",
                            new Vector3(cellMinX + TileStep + WallThickness * 0.5f, cellMinY + TileStep * 0.5f, 0f),
                            new Vector2(WallThickness, TileStep));

                    if (y == 0 || !floor[x, y - 1])
                        CreateWall(wallsRoot.transform, "Wall",
                            new Vector3(cellMinX + TileStep * 0.5f, cellMinY - WallThickness * 0.5f, 0f),
                            new Vector2(TileStep, WallThickness));

                    if (y == rows - 1 || !floor[x, y + 1])
                        CreateWall(wallsRoot.transform, "Wall",
                            new Vector3(cellMinX + TileStep * 0.5f, cellMinY + TileStep + WallThickness * 0.5f, 0f),
                            new Vector2(TileStep, WallThickness));
                }
            }
        }

        private static void MarkFloorCells(bool[,] floor, Rect area, float originX, float originY, int cols, int rows)
        {
            int startX = Mathf.FloorToInt((area.xMin - originX) / TileStep);
            int endX = Mathf.CeilToInt((area.xMax - originX) / TileStep);
            int startY = Mathf.FloorToInt((area.yMin - originY) / TileStep);
            int endY = Mathf.CeilToInt((area.yMax - originY) / TileStep);

            for (int y = startY; y < endY; y++)
            {
                if (y < 0 || y >= rows) continue;
                for (int x = startX; x < endX; x++)
                {
                    if (x < 0 || x >= cols) continue;
                    float cx = originX + x * TileStep + TileStep * 0.5f;
                    float cy = originY + y * TileStep + TileStep * 0.5f;
                    if (area.Contains(new Vector2(cx, cy)))
                        floor[x, y] = true;
                }
            }
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.layer = GameLayers.Environment;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EnemySpriteLoader.CreatePlaceholder(WallColor, 16);
            sr.sortingLayerName = SortingLayers.EnvironmentFront;
            sr.sortingOrder = 5;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            go.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        private static Transform CreateExit(Transform parent, Vector3 position)
        {
            var exitGo = new GameObject("LobbyExit");
            exitGo.transform.SetParent(parent, false);
            exitGo.transform.position = position;
            exitGo.layer = GameLayers.Environment;
            var exitCol = exitGo.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector2(4f, 1.2f);

            var exitLabel = new GameObject("ExitLabel");
            exitLabel.transform.SetParent(exitGo.transform, false);
            exitLabel.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var tm = exitLabel.AddComponent<TextMesh>();
            tm.text = "EXIT";
            tm.fontSize = 48;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.95f, 0.85f, 0.35f, 1f);
            var mr = exitLabel.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = SortingLayers.WorldUI;
                mr.sortingOrder = 20;
            }

            return exitGo.transform;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }
    }
}
