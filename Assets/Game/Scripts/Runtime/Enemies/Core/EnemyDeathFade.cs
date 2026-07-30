using System.Collections;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Fades an enemy sprite out on death, then destroys the GameObject.
    /// </summary>
    public static class EnemyDeathFade
    {
        public static void Begin(MonoBehaviour host, float duration = 0.55f)
        {
            if (host == null)
                return;
            host.StartCoroutine(FadeRoutine(host.gameObject, duration));
        }

        private static IEnumerator FadeRoutine(GameObject go, float duration)
        {
            if (go == null)
                yield break;

            foreach (var col in go.GetComponentsInChildren<Collider2D>(true))
                col.enabled = false;

            var body = go.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }

            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            var startColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                startColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

            duration = Mathf.Max(0.05f, duration);
            float t = 0f;
            while (t < duration && go != null)
            {
                t += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(t / duration);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                        continue;
                    var c = startColors[i];
                    c.a = startColors[i].a * a;
                    renderers[i].color = c;
                }

                yield return null;
            }

            if (go != null)
                Object.Destroy(go);
        }
    }
}
