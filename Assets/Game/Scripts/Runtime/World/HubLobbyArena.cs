using RealmShards.Core;
using RealmShards.Rooms;
using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Small tiled lobby room with an open northern exit.
    /// </summary>
    public static class HubLobbyArena
    {
        public const float RoomWidth = 18f;
        public const float RoomHeight = 14f;

        public struct LobbyArenaResult
        {
            public Transform Root;
            public RoomBounds Bounds;
            public Transform ExitTrigger;
            public Transform TomeSpawn;
            public Vector3[] PlayerSpawns;
        }

        public static LobbyArenaResult Build(Transform parent = null)
        {
            var roomSize = new Vector2(RoomWidth, RoomHeight);
            var baseArena = ArenaBuilder.Build(roomSize, parent);
            if (baseArena.ExitBlockers != null)
            {
                for (int i = 0; i < baseArena.ExitBlockers.Length; i++)
                {
                    if (baseArena.ExitBlockers[i] != null)
                        Object.Destroy(baseArena.ExitBlockers[i].gameObject);
                }
            }

            float halfH = roomSize.y * 0.5f;
            var exitGo = new GameObject("LobbyExit");
            exitGo.transform.SetParent(baseArena.Root, false);
            exitGo.transform.position = new Vector3(0f, halfH - 0.35f, 0f);
            exitGo.layer = GameLayers.Environment;
            var exitCol = exitGo.AddComponent<BoxCollider2D>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector2(4.5f, 1.2f);

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

            var tomeGo = new GameObject("TomeSpawn");
            tomeGo.transform.SetParent(baseArena.Root, false);
            tomeGo.transform.position = new Vector3(0f, -1.2f, 0f);

            var spawns = new[]
            {
                new Vector3(-4.5f, -3.5f, 0f),
                new Vector3(4.5f, -3.5f, 0f),
                new Vector3(-4.5f, 1.5f, 0f),
                new Vector3(4.5f, 1.5f, 0f)
            };

            if (baseArena.Root != null)
                baseArena.Root.name = "HubLobbyArena";

            return new LobbyArenaResult
            {
                Root = baseArena.Root,
                Bounds = baseArena.Bounds,
                ExitTrigger = exitGo.transform,
                TomeSpawn = tomeGo.transform,
                PlayerSpawns = spawns
            };
        }
    }
}
