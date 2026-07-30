using System.Collections;
using UnityEngine;

namespace RealmShards.Combat
{
    /// <summary>
    /// Brief global hit-stop. Safe across scenes — restores timescale.
    /// </summary>
    public static class HitStop
    {
        private static MonoBehaviour _host;
        private static Coroutine _routine;
        private static float _resumeScale = 1f;
        private static bool _pausedByMenu;

        public static void SetMenuPaused(bool paused)
        {
            _pausedByMenu = paused;
            if (paused)
            {
                if (_routine != null && _host != null)
                {
                    _host.StopCoroutine(_routine);
                    _routine = null;
                }
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _resumeScale > 0.01f ? _resumeScale : 1f;
            }
        }

        public static void Request(float duration, float scale = 0.05f)
        {
            if (_pausedByMenu || duration <= 0f)
                return;

            EnsureHost();
            if (_routine != null)
                _host.StopCoroutine(_routine);
            _routine = _host.StartCoroutine(Run(duration, scale));
        }

        /// <summary>Call on scene unload / quit to hub so timeScale never sticks at 0.</summary>
        public static void EnsureRunningTimeScale()
        {
            _pausedByMenu = false;
            if (_routine != null && _host != null)
            {
                _host.StopCoroutine(_routine);
                _routine = null;
            }
            if (Time.timeScale < 0.01f)
                Time.timeScale = 1f;
            _resumeScale = 1f;
        }

        private static void EnsureHost()
        {
            if (_host != null) return;
            var go = new GameObject("HitStopHost");
            Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<HitStopHost>();
        }

        private static IEnumerator Run(float duration, float scale)
        {
            _resumeScale = Time.timeScale > 0.01f ? Time.timeScale : 1f;
            Time.timeScale = Mathf.Clamp(scale, 0.01f, 1f);
            yield return new WaitForSecondsRealtime(duration);
            if (!_pausedByMenu)
                Time.timeScale = _resumeScale > 0.01f ? _resumeScale : 1f;
            _routine = null;
        }

        private sealed class HitStopHost : MonoBehaviour
        {
            private void OnDestroy()
            {
                if (Time.timeScale < 0.01f && !_pausedByMenu)
                    Time.timeScale = 1f;
            }
        }
    }
}
