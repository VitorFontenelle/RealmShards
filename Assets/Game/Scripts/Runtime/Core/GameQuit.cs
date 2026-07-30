using UnityEngine;

namespace RealmShards.Core
{
    public static class GameQuit
    {
        public static void Request()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
