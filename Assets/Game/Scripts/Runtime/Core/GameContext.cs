using RealmShards.Progression;
using RealmShards.Runs;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RealmShards.Core
{
    /// <summary>
    /// Composition root. Lives in DontDestroyOnLoad. Prefer this over FindObjectOfType in Update.
    /// </summary>
    public sealed class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        [SerializeField] private ContentDatabase contentDatabase;

        private ISaveService _save;
        private ProgressionService _progression;
        private IRunHost _runHost;
        private bool _bootstrapped;

        public ISaveService Save => _save;
        public ProgressionService Progression => _progression;
        public IRunHost Runs => _runHost;
        public RunSession RunSession => _runHost?.Session;
        public ContentDatabase Content => contentDatabase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null)
            {
                return;
            }

            var go = new GameObject(nameof(GameContext));
            DontDestroyOnLoad(go);
            go.AddComponent<GameContext>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _save = new JsonSaveService();
            _save.LoadOrCreate();
            _progression = new ProgressionService(_save);
            _runHost = new RunHost(_save, _progression);

            if (contentDatabase == null)
            {
                contentDatabase = ContentDatabase.CreateRuntimeDefault();
            }
            else
            {
                contentDatabase.RebuildLookup();
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                Instance = null;
            }
        }

        private void Start()
        {
            // Handle the scene that was already active when we were created.
            HandleScene(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HandleScene(scene);
        }

        private void HandleScene(Scene scene)
        {
            switch (scene.name)
            {
                case SceneNames.Bootstrap:
                    RunBootstrap();
                    break;
                case SceneNames.Hub:
                    EnsureEventSystem();
                    UI.HubScreen.EnsurePresent();
                    break;
                case SceneNames.RunResults:
                    EnsureEventSystem();
                    UI.RunResultsScreen.EnsurePresent();
                    break;
                case SceneNames.CityRun:
                    EnsureEventSystem();
                    // Prefer world bootstrap + meta bridge; stub only if neither exists yet.
                    UI.CityRunMetaBridge.EnsurePresent();
                    UI.CityRunStubScreen.EnsurePresent();
                    break;
            }
        }

        private void RunBootstrap()
        {
            if (_bootstrapped)
            {
                return;
            }

            _bootstrapped = true;
            _save.LoadOrCreate();
            Debug.Log($"[GameContext] Save loaded. Year={_progression.Year} Decade={_progression.Decade} Vestiges={_progression.ArcaneVestiges} Path={_save.SaveFilePath}");
            SceneManager.LoadScene(SceneNames.Hub);
        }

        public static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            // Project uses Input System only (activeInputHandler=1).
            es.AddComponent<InputSystemUIInputModule>();
        }
    }
}
