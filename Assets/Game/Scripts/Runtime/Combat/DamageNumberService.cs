using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Combat
{
    /// <summary>
    /// Pooled floating damage numbers (world-space TextMesh).
    /// </summary>
    public sealed class DamageNumberService : MonoBehaviour
    {
        private static DamageNumberService _instance;
        private readonly Queue<TextMesh> _pool = new Queue<TextMesh>(32);
        private Transform _root;

        public static DamageNumberService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<DamageNumberService>();
                    if (_instance == null)
                    {
                        var go = new GameObject("DamageNumberService");
                        _instance = go.AddComponent<DamageNumberService>();
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
            _root = transform;
            for (int i = 0; i < 16; i++)
                _pool.Enqueue(CreateOne());
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        public static void Spawn(Vector3 worldPos, float amount, bool critical = false)
        {
            Instance.Show(worldPos, amount, critical);
        }

        public void Show(Vector3 worldPos, float amount, bool critical = false)
        {
            var tm = _pool.Count > 0 ? _pool.Dequeue() : CreateOne();
            tm.gameObject.SetActive(true);
            tm.transform.position = worldPos + Vector3.up * 0.4f;
            int shown = Mathf.Max(1, Mathf.RoundToInt(amount));
            tm.text = shown.ToString();
            tm.color = critical ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.92f, 0.92f);
            tm.characterSize = critical ? 0.22f : 0.16f;
            StartCoroutine(Animate(tm));
        }

        private System.Collections.IEnumerator Animate(TextMesh tm)
        {
            Vector3 start = tm.transform.position;
            Color c = tm.color;
            float t = 0f;
            const float dur = 0.65f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float a = t / dur;
                tm.transform.position = start + Vector3.up * (0.8f * a);
                c.a = 1f - a;
                tm.color = c;
                yield return null;
            }

            tm.gameObject.SetActive(false);
            _pool.Enqueue(tm);
        }

        private TextMesh CreateOne()
        {
            var go = new GameObject("DmgNum");
            go.transform.SetParent(_root);
            go.SetActive(false);
            var tm = go.AddComponent<TextMesh>();
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 48;
            tm.characterSize = 0.16f;
            tm.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = Core.SortingLayers.SkillEffectsFront;
                mr.sortingOrder = 50;
            }
            return tm;
        }
    }
}
