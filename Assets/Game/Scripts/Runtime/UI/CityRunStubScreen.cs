using RealmShards.Core;
using RealmShards.Runs;
using RealmShards.Save;
using UnityEngine;

namespace RealmShards.UI
{
    /// <summary>
    /// Full-screen CityRun stand-in used only when meta bridge is absent.
    /// </summary>
    public sealed class CityRunStubScreen : MonoBehaviour
    {
        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<CityRunStubScreen>() != null)
            {
                return;
            }

            if (FindFirstObjectByType<CityRunMetaBridge>() != null)
            {
                return;
            }

            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is ICityRunReady)
                {
                    return;
                }
            }

            var canvas = UiFactory.CreateScreenCanvas("CityRunStubUI", 50);
            canvas.gameObject.AddComponent<CityRunStubScreen>();
        }

        private void Start()
        {
            var root = transform;
            UiFactory.AddPanel(root, "Background", new Color(0.1f, 0.12f, 0.14f, 0.92f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            UiFactory.AddText(root, "Info",
                "CityRun stub\n(World bootstrap missing)",
                28, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.85f), Vector2.zero, Vector2.zero);

            var win = UiFactory.AddButton(root, "Win", "Win Run (+25 Vestiges)",
                new Vector2(0.15f, 0.18f), new Vector2(0.48f, 0.30f), Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.45f, 0.28f, 1f));
            win.onClick.AddListener(() => End(success: true));

            var fail = UiFactory.AddButton(root, "Fail", "Fail Run (+10 Year)",
                new Vector2(0.52f, 0.18f), new Vector2(0.85f, 0.30f), Vector2.zero, Vector2.zero,
                new Color(0.5f, 0.18f, 0.18f, 1f));
            fail.onClick.AddListener(() => End(success: false));
        }

        private static void End(bool success)
        {
            var ctx = GameContext.Instance;
            if (ctx == null)
            {
                return;
            }

            var s = ctx.RunSession;
            var city = s?.CityId ?? ContentIdDefaults.CityStarter;
            var route = s?.RouteId ?? ContentIdDefaults.RouteStarterMain;
            ctx.Runs.EndRun(success
                ? RunOutcome.Success(city, route, 25)
                : RunOutcome.Failure(city, route));
        }
    }

    /// <summary>
    /// Implement on a world-agent MonoBehaviour in CityRun to suppress the meta stub UI.
    /// </summary>
    public interface ICityRunReady
    {
    }
}
