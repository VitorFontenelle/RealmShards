using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShards
{
    /// <summary>
    /// Local multiplayer join / test spawner. CityRun uses <see cref="World.CityRunBootstrap"/> for level entry.
    /// </summary>
    public sealed class PlayerJoinSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool spawnPlayerOnStart = true;
        [SerializeField] private bool ensurePoolHub = true;
        [SerializeField] private int maxPlayers = 4;

        private int _spawned;

        private void Awake()
        {
            if (ensurePoolHub && FindFirstObjectByType<PoolHub>() == null)
            {
                var hub = new GameObject("PoolHub");
                hub.AddComponent<PoolHub>();
            }
        }

        private void Start()
        {
            if (spawnPlayerOnStart && _spawned == 0 && playerPrefab != null)
            {
                SpawnPlayer(0, null);
            }
        }

        public GameObject SpawnPlayer(int playerIndex, InputDevice device)
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[PlayerJoinSpawner] Missing playerPrefab.");
                return null;
            }

            if (_spawned >= maxPlayers)
            {
                return null;
            }

            Vector3 pos = GetSpawnPosition(playerIndex);
            var instance = Instantiate(playerPrefab, pos, Quaternion.identity);
            instance.name = $"Player_{playerIndex + 1}";

            var controller = instance.GetComponent<PlayerController>();
            controller?.InitializePlayer(playerIndex);

            var pi = instance.GetComponent<PlayerInput>();
            if (pi != null && device != null)
            {
                pi.SwitchCurrentControlScheme(device);
            }

            _spawned++;
            return instance;
        }

        public void OnPlayerJoined(PlayerInput input)
        {
            int index = input != null ? input.playerIndex : _spawned;
            Vector3 pos = GetSpawnPosition(index);
            input.transform.position = pos;

            var controller = input.GetComponent<PlayerController>();
            controller?.InitializePlayer(index);
            _spawned = Mathf.Max(_spawned, index + 1);
        }

        public void OnPlayerLeft(PlayerInput input)
        {
            if (input != null)
            {
                Destroy(input.gameObject);
            }
        }

        private Vector3 GetSpawnPosition(int index)
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var t = spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
                if (t != null)
                {
                    return t.position;
                }
            }

            return transform.position + new Vector3(index * 1.25f, 0f, 0f);
        }
    }
}
