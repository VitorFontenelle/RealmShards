using System;
using System.Collections;
using RealmShards.Core;
using RealmShards.Enemies;
using RealmShards.Runs;
using UnityEngine;

namespace RealmShards.Rooms
{
    /// <summary>
    /// Chains trash rooms then champion within a single CityRun scene load.
    /// First room clear must NOT end the world run — only city completion does.
    /// </summary>
    public sealed class CityRunDirector : MonoBehaviour
    {
        [SerializeField] private float interRoomDelay = 1.25f;
        [SerializeField] private EncounterDefinition trashEncounter;
        [SerializeField] private EncounterDefinition championEncounter;
        [SerializeField] private CoopScalingConfig coopScaling;

        private World.ArenaBuilder.ArenaResult _arena;
        private EncounterRoom _room;
        private CityRoomPlanner.Plan _plan;
        private int _roomIndex;
        private bool _cityDone;
        private Coroutine _advanceRoutine;

        public int RoomIndex => _roomIndex;
        public int TotalRooms => _plan.TotalRooms;
        public bool IsChampionRoom => _plan.IsChampionRoom(_roomIndex);
        public bool IsCityComplete => _cityDone;
        public EncounterRoom ActiveRoom => _room;

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
            _roomIndex = Mathf.Clamp(session?.RoomIndex ?? 0, 0, _plan.TotalRooms - 1);
            _cityDone = false;
            StartRoom(_roomIndex);
        }

        private void StartRoom(int index)
        {
            _roomIndex = index;
            if (GameContext.Instance?.RunSession != null)
                GameContext.Instance.RunSession.RoomIndex = index;

            if (_room != null)
            {
                _room.Cleared -= OnRoomCleared;
                Destroy(_room.gameObject);
                _room = null;
            }

            var go = new GameObject($"EncounterRoom_{index}");
            go.transform.SetParent(transform);
            _room = go.AddComponent<EncounterRoom>();

            bool champion = _plan.IsChampionRoom(index);
            EncounterDefinition def = champion
                ? (championEncounter != null ? championEncounter : BuildRuntimeChampionEncounter())
                : (trashEncounter != null ? trashEncounter : BuildRuntimeTrashEncounter(index));

            // Ensure trash never spawns champion; champion room always does.
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
                    def != null ? def.EncounterId : $"city-trash-{index}",
                    def != null ? CopySpawns(def) : DefaultTrashSpawns(index),
                    null,
                    false,
                    $"city-trash-{index}");
            }

            var scaling = coopScaling != null
                ? coopScaling
                : ScriptableObject.CreateInstance<CoopScalingConfig>();

            _room.Configure(runtime, scaling, _arena.Bounds, _arena.EnemySpawns, _arena.ChampionSpawns);
            _room.SetExitBlockers(_arena.ExitBlockers);
            _room.Cleared += OnRoomCleared;
            _room.BeginEncounter();
            RoomStarted?.Invoke(_roomIndex, _plan.TotalRooms);
            Debug.Log($"[CityRunDirector] Room {_roomIndex + 1}/{_plan.TotalRooms} ({(champion ? "champion" : "trash")})");
        }

        private void OnRoomCleared(EncounterRoom room)
        {
            RoomCleared?.Invoke(_roomIndex, _plan.TotalRooms);

            if (_plan.IsFinalRoom(_roomIndex))
            {
                CompleteCity();
                return;
            }

            if (_advanceRoutine != null)
                StopCoroutine(_advanceRoutine);
            _advanceRoutine = StartCoroutine(AdvanceAfterDelay());
        }

        private IEnumerator AdvanceAfterDelay()
        {
            yield return new WaitForSecondsRealtime(interRoomDelay);
            // Prefer scaled time once pause is cleared; fall back if timeScale 0.
            if (Time.timeScale > 0.01f)
                yield return new WaitForSeconds(0.15f);

            StartRoom(_roomIndex + 1);
            _advanceRoutine = null;
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

        private void OnDestroy()
        {
            if (_room != null)
                _room.Cleared -= OnRoomCleared;
        }
    }
}
