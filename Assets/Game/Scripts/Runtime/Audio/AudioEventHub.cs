using UnityEngine;

namespace RealmShards.Audio
{
    /// <summary>
    /// Lightweight audio event stubs — silent by default, hooks for future clips.
    /// </summary>
    public sealed class AudioEventHub : MonoBehaviour
    {
        private static AudioEventHub _instance;
        private AudioSource _source;

        [SerializeField] private bool logEvents;

        public static AudioEventHub Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<AudioEventHub>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioEventHub");
                        Object.DontDestroyOnLoad(go);
                        _instance = go.AddComponent<AudioEventHub>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null)
                _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public static void Play(string eventName, Vector3 worldPos = default)
        {
            Instance.PlayInternal(eventName, worldPos);
        }

        private void PlayInternal(string eventName, Vector3 worldPos)
        {
            if (logEvents)
                Debug.Log($"[Audio] {eventName} @ {worldPos}");
            // Placeholder: no clip assigned — keeps AudioSource ready for content.
            _ = eventName;
            if (_source != null && worldPos != default)
                _source.transform.position = worldPos;
        }
    }

    [CreateAssetMenu(menuName = "RealmShards/Audio/Audio Event Catalog", fileName = "AudioEventCatalog")]
    public sealed class AudioEventCatalog : ScriptableObject
    {
        [SerializeField] private string[] eventNames =
        {
            "ability.cast",
            "combat.hit",
            "combat.heavy_hit",
            "enemy.death",
            "champion.death",
            "ui.pause",
            "ui.resume"
        };

        public string[] EventNames => eventNames;
    }
}
