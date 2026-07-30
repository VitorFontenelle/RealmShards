using UnityEngine;

namespace RealmShards.World
{
    /// <summary>
    /// Reveals dungeon cells around living players for fog-of-war minimap.
    /// </summary>
    public sealed class ExplorationFog : MonoBehaviour
    {
        [SerializeField] private int revealRadius = 4;

        private DungeonMap _map;
        private bool[] _explored;

        public DungeonMap Map => _map;
        public int RevealRadius => revealRadius;

        public void Configure(DungeonMap map, int radius = 4)
        {
            _map = map;
            revealRadius = Mathf.Max(1, radius);
            _explored = map != null ? new bool[map.Width * map.Height] : null;

            // Seed reveal around the first room so the minimap isn't fully black at spawn.
            if (map != null && map.Rooms.Count > 0)
                RevealAround(map.Rooms[0].PlayerSpawn);
        }

        public bool IsExplored(int x, int y)
        {
            if (_map == null || _explored == null || !_map.InBounds(x, y))
                return false;
            return _explored[_map.Index(x, y)];
        }

        public void RevealAround(Vector3 worldPos)
        {
            if (_map == null || _explored == null)
                return;

            var c = _map.WorldToCell(worldPos);
            int r = revealRadius;
            for (int y = c.y - r; y <= c.y + r; y++)
            for (int x = c.x - r; x <= c.x + r; x++)
            {
                if (!_map.InBounds(x, y))
                    continue;
                int dx = x - c.x;
                int dy = y - c.y;
                if (dx * dx + dy * dy > r * r)
                    continue;
                _explored[_map.Index(x, y)] = true;
            }
        }

        private void Update()
        {
            if (_map == null)
                return;

            var players = Enemies.PlayerTargetRegistry.Collect();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].IsAlive && players[i].Transform != null)
                    RevealAround(players[i].Transform.position);
            }
        }
    }
}
