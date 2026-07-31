using RealmShards.Input;
using RealmShards.Progression;
using RealmShards.Runs;
using RealmShards.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace RealmShards.Core
{
    /// <summary>
    /// Composition root. Lives in DontDestroyOnLoad.
    /// </summary>
    public sealed class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        [SerializeField] private ContentDatabase contentDatabase;
        [SerializeField] private InputActionAsset inputActions;

        private ISaveService _save;
        private ProgressionService _progression;
        private IRunHost _runHost;
        private BindingOverridesService _bindings;
        private LocalCoopLobby _lobby;
        private bool _bootstrapped;

        public ISaveService Save => _save;
        public ProgressionService Progression => _progression;
        public IRunHost Runs => _runHost;
        public RunSession RunSession => _runHost?.Session;
        public ContentDatabase Content => contentDatabase;
        public InputActionAsset InputActions => inputActions;
        public BindingOverridesService Bindings => _bindings;
        public LocalCoopLobby Lobby => _lobby;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
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

            Application.runInBackground = true;

            _save = new JsonSaveService();
            _save.LoadOrCreate();
            _progression = new ProgressionService(_save);
            _runHost = new RunHost(_save, _progression);
            _lobby = new LocalCoopLobby();

            if (inputActions == null)
            {
#if UNITY_EDITOR
                inputActions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    "Assets/Game/Settings/RealmShards.inputactions");
#endif
                if (inputActions == null)
                    inputActions = Resources.Load<InputActionAsset>("RealmShards");
            }

            if (inputActions != null)
            {
                _bindings = new BindingOverridesService(inputActions);
                _bindings.Load();
            }

            if (contentDatabase == null)
                contentDatabase = ContentDatabase.CreateRuntimeDefault();
            else
                contentDatabase.RebuildLookup();

            SettingsService.Initialize(_save);

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

        private void Start() => HandleScene(SceneManager.GetActiveScene());

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HandleScene(scene);

        private void HandleScene(Scene scene)
        {
            Combat.HitStop.EnsureRunningTimeScale();
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
                    UI.CityRunMetaBridge.EnsurePresent();
                    UI.CityRunStubScreen.EnsurePresent();
                    break;
            }
        }

        private void RunBootstrap()
        {
            if (_bootstrapped) return;
            _bootstrapped = true;
            _save.LoadOrCreate();
            Debug.Log($"[GameContext] Save loaded. Year={_progression.Year} Decade={_progression.Decade} Vestiges={_progression.ArcaneVestiges}");
            SceneManager.LoadScene(SceneNames.Hub);
        }

        public static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            if (Instance != null && Instance.inputActions != null)
            {
                // Fall back to module defaults if no UI map — still gamepad-navigable via built-in.
                module.actionsAsset = Instance.inputActions;
            }
        }
    }
}
