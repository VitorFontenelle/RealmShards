using System.Collections.Generic;
using UnityEngine;

namespace RealmShards.Enemies
{
    /// <summary>
    /// Discovers living players for AI targeting (frame-cached).
    /// </summary>
    public static class PlayerTargetRegistry
    {
        private static readonly List<IPlayerMarker> Buffer = new List<IPlayerMarker>(8);
        private static int _cachedFrame = -1;

        public static IReadOnlyList<IPlayerMarker> Collect()
        {
            if (_cachedFrame == Time.frameCount)
                return Buffer;

            _cachedFrame = Time.frameCount;
            Buffer.Clear();

            var proxies = Object.FindObjectsByType<Combat.PlayerTargetProxy>(FindObjectsSortMode.None);
            for (int i = 0; i < proxies.Length; i++)
            {
                if (proxies[i] != null && proxies[i].IsAlive)
                    Buffer.Add(proxies[i]);
            }

            if (Buffer.Count == 0)
            {
                var identities = Object.FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);
                for (int i = 0; i < identities.Length; i++)
                {
                    var id = identities[i];
                    if (id == null)
                        continue;
                    var health = id.GetComponent<Health>();
                    if (health != null && !health.IsAlive)
                        continue;
                    Buffer.Add(new PlayerTargetAdapter(id.transform, health));
                }
            }

            if (Buffer.Count == 0)
            {
                GameObject[] tagged;
                try { tagged = GameObject.FindGameObjectsWithTag("Player"); }
                catch { tagged = System.Array.Empty<GameObject>(); }

                for (int i = 0; i < tagged.Length; i++)
                {
                    var go = tagged[i];
                    if (go == null)
                        continue;
                    var health = go.GetComponent<Health>();
                    if (health != null && !health.IsAlive)
                        continue;
                    Buffer.Add(new PlayerTargetAdapter(go.transform, health));
                }
            }

            return Buffer;
        }

        public static int CountAlive()
        {
            Collect();
            int n = 0;
            for (int i = 0; i < Buffer.Count; i++)
            {
                if (Buffer[i] != null && Buffer[i].IsAlive)
                    n++;
            }
            return Mathf.Max(1, n);
        }
    }
}
