using System;
using System.Collections.Generic;
using RealmShards.Core;
using RealmShards.Enemies;
using RealmShards.Runs;
using UnityEngine;

namespace RealmShards.Rooms
{
    /// <summary>
    /// Spawns trash/champion encounters across physical dungeon rooms.
    /// Rooms are explored freely; each room's enemies live until cleared.
    /// </summary>
    public sealed class CityRunDirector : MonoBehaviour
    {
        [SerializeField] private EncounterDefinition trashEncounter;
        [SerializeField] private EncounterDefinition championEncounter;
        [SerializeField] private CoopScalingConfig coopScaling;

        private World.ArenaBuilder.ArenaResult _arena;
        private CityRoomPlanner.Plan _plan;
        private readonly List<EncounterRoom> _rooms = new List<EncounterRoom>(6);
        private readonly HashSet<int> _cleared = new HashSet<int>();
        private bool _cityDone;
        private int _activeRoomIndex;

        public int RoomIndex => _activeRoomIndex;
        public int TotalRooms => _plan.TotalRooms;
        public bool IsChampionRoom => _plan.IsChampionRoom(_activeRoomIndex);
        public bool IsCityComplete => _cityDone;
        public EncounterRoom ActiveRoom =>
            _activeRoomIndex >= 0 && _activeRoomIndex < _rooms.Count ? _rooms[_activeRoomIndex] : null;

        public event Action<int, int> RoomStarted;
        public event Action<int, int> RoomCleared;
        public event Action CityCompleted;

        public void Configure(
            World.ArenaBuilder.ArenaResult arena,
            EncounterDefinition trash,
            EncounterDefinition champion,
            CoopScalingConfig scaling)
        {
            _arena = arena;
            trashEncounter = trash;
            championEncounter = champion;
            coopScaling = scaling;
        }

        public void Begin()
        {
            var session = GameContext.Instance?.RunSession;
            int seed = session?.Seed ?? 1;
            int node = session?.WorldNodeIndex ?? 0;
            bool capital = session?.IsCapitalNode == true;
            _plan = CityRoomPlanner.Build(seed, node, capital);
            _cityDone = false;
            _cleared.Clear();
            _activeRoomIndex = 0;

            SpawnAllRoomEncounters();
            RoomStarted?.Invoke(0, _plan.TotalRooms);
        }

        private void SpawnAllRoomEncounters()
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i] != null)
                    Destroy(_rooms[i].gameObject);
            }
            _rooms.Clear();

            int physicalRooms = _arena.Map != null ? _arena.Map.Rooms.Count : 1;
            int count = Mathf.Max(1, Mathf.Min(_plan.TotalRooms, physicalRooms));

            for (int i = 0; i < count; i++)
            {
                bool champion = _plan.IsChampionRoom(i);
                EncounterDefinition def = champion
                    ? (championEncounter != null ? championEncounter : BuildRuntimeChampionEncounter())
                    : (trashEncounter != null ? trashEncounter : BuildRuntimeTrashEncounter(i));

                var runtime = ScriptableObject.CreateInstance<EncounterDefinition>();
                if (champion)
                {
                    runtime.SetRuntime(
                        def != null ? def.EncounterId : "city-champion",
                        def != null ? CopySpawns(def) : LightTrashSpawns(),
                        ResolveChampionEnemy(def),
                        true,
                        "city-champion-clear");
                }
                else
                {
                    runtime.SetRuntime(
                        def != null ? def.EncounterId : $"city-trash-{i}",
                        def != null ? CopySpawns(def) : DefaultTrashSpawns(i),
                        null,
                        false,
                        $"city-trash-{i}");
                }

                var scaling = coopScaling != null
                    ? coopScaling
                    : ScriptableObject.CreateInstance<CoopScalingConfig>();

                CollectSpawnsForRoom(i, out var enemySpawns, out var champSpawns);

                var go = new GameObject($"EncounterRoom_{i}");
                go.transform.SetParent(transform);
                var room = go.AddComponent<EncounterRoom>();
                room.Configure(runtime, scaling, _arena.Bounds, enemySpawns, champSpawns);
                room.SetExitBlockers(_arena.ExitBlockers);
                int captured = i;
                room.Cleared += r => OnRoomClearedIndex(captured, r);
                room.BeginEncounter();
                _rooms.Add(room);
            }

            Debug.Log($"[CityRunDirector] Spawned {count} physical room encounters.");
        }

        private void CollectSpawnsForRoom(int roomIndex, out List<SpawnPoint> enemies, out List<SpawnPoint> champions)
        {
            enemies = new List<SpawnPoint>();
            champions = new List<SpawnPoint>();

            if (_arena.Map != null && roomIndex < _arena.Map.Rooms.Count)
            {
                var info = _arena.Map.Rooms[roomIndex];
                if (_arena.EnemySpawns != null)
                {
                    string prefix = $"EnemySpawn_R{roomIndex}_";
                    for (int i = 0; i < _arena.EnemySpawns.Count; i++)
                    {
                        var sp = _arena.EnemySpawns[i];
                        if (sp != null && sp.gameObject.name.StartsWith(prefix, StringComparison.Ordinal))
                            enemies.Add(sp);
                    }
                }

                if (enemies.Count == 0)
                {
                    for (int e = 0; e < info.EnemySpawns.Count; e++)
                    {
                        var go = new GameObject($"RuntimeEnemySpawn_R{roomIndex}_{e}");
                        go.transform.SetParent(transform);
                        go.transform.position = info.EnemySpawns[e];
                        var sp = go.AddComponent<SpawnPoint>();
                        sp.Configure(SpawnPointKind.Enemy, go.name);
                        enemies.Add(sp);
                    }
                }

                if (info.IsChampion && _arena.ChampionSpawns != null && _arena.ChampionSpawns.Count > 0)
                    champions.AddRange(_arena.ChampionSpawns);
                else if (info.IsChampion)
                {
                    var go = new GameObject($"RuntimeChampionSpawn_R{roomIndex}");
                    go.transform.SetParent(transform);
                    go.transform.position = info.ChampionSpawn;
                    var sp = go.AddComponent<SpawnPoint>();
                    sp.Configure(SpawnPointKind.Champion, go.name);
                    champions.Add(sp);
                }

                return;
            }

            if (_arena.EnemySpawns != null)
                enemies.AddRange(_arena.EnemySpawns);
            if (_arena.ChampionSpawns != null)
                champions.AddRange(_arena.ChampionSpawns);
        }

        private void OnRoomClearedIndex(int index, EncounterRoom room)
        {
            _cleared.Add(index);
            _activeRoomIndex = index;
            RoomCleared?.Invoke(index, _plan.TotalRooms);

            if (_cleared.Count >= _rooms.Count)
                CompleteCity();
            else
            {
                for (int i = 0; i < _rooms.Count; i++)
                {
                    if (!_cleared.Contains(i))
                    {
                        _activeRoomIndex = i;
                        RoomStarted?.Invoke(_activeRoomIndex, _plan.TotalRooms);
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (_arena.Map == null || _cityDone)
                return;

            var players = PlayerTargetRegistry.Collect();
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == null || !players[i].IsAlive || players[i].Transform == null)
                    continue;
                var room = _arena.Map.FindRoomAt(players[i].Transform.position);
                if (room != null)
                    _activeRoomIndex = room.Index;
            }
        }

        private void CompleteCity()
        {
            if (_cityDone) return;
            _cityDone = true;
            var session = GameContext.Instance?.RunSession;
            if (session != null)
                session.AwaitingArcaneCore = true;
            CityCompleted?.Invoke();
            Debug.Log("[CityRunDirector] City encounters complete — awaiting Arcane Core / meta advance.");
        }

        private static EncounterDefinition.EnemySpawnEntry[] CopySpawns(EncounterDefinition def)
        {
            if (def?.Spawns == null || def.Spawns.Count == 0)
                return DefaultTrashSpawns(0);
            var arr = new EncounterDefinition.EnemySpawnEntry[def.Spawns.Count];
            for (int i = 0; i < def.Spawns.Count; i++)
                arr[i] = def.Spawns[i];
            return arr;
        }

        private static EncounterDefinition.EnemySpawnEntry[] DefaultTrashSpawns(int roomIndex)
        {
            int warriors = 1 + (roomIndex % 2);
            int archers = 1 + ((roomIndex + 1) % 2);
            return new[]
            {
                new EncounterDefinition.EnemySpawnEntry
                {
                    archetypeFallback = EnemyArchetype.Warrior,
                    count = warriors
                },
                new EncounterDefinition.EnemySpawnEntry
                {
                    archetypeFallback = EnemyArchetype.Archer,
                    count = archers
                }
            };
        }

        private static EncounterDefinition.EnemySpawnEntry[] LightTrashSpawns()
        {
            return new[]
            {
                new EncounterDefinition.EnemySpawnEntry
                {
                    archetypeFallback = EnemyArchetype.Warrior,
                    count = 1
                }
            };
        }

        private EnemyDefinition ResolveChampionEnemy(EncounterDefinition def)
        {
            var year = GameContext.Instance?.Progression?.Year ?? 0;
            var seed = GameContext.Instance?.RunSession?.Seed ?? 1;
            var picked = ChampionSelector.Pick(seed, year);
            if (picked?.EnemyDefinition != null)
                return picked.EnemyDefinition;
            if (def != null && def.ChampionDefinition != null)
                return def.ChampionDefinition;
            return null;
        }

        private EncounterDefinition BuildRuntimeTrashEncounter(int index)
        {
            var e = ScriptableObject.CreateInstance<EncounterDefinition>();
            e.SetRuntime($"runtime-trash-{index}", DefaultTrashSpawns(index), null, false, "trash");
            return e;
        }

        private EncounterDefinition BuildRuntimeChampionEncounter()
        {
            var e = ScriptableObject.CreateInstance<EncounterDefinition>();
            e.SetRuntime("runtime-champion", LightTrashSpawns(), null, true, "champion");
            return e;
        }
    }
}
