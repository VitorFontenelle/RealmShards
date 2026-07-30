using RealmShards.Core;
using RealmShards.Runs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RealmShards.UI
{
    /// <summary>
    /// Shows last run outcome. Failure already advanced year in RunHost.
    /// </summary>
    public sealed class RunResultsScreen : MonoBehaviour
    {
        public static void EnsurePresent()
        {
            if (FindFirstObjectByType<RunResultsScreen>() != null)
            {
                return;
            }

            var canvas = UiFactory.CreateScreenCanvas("RunResultsUI");
            canvas.gameObject.AddComponent<RunResultsScreen>();
        }

        private void Start()
        {
            var root = transform;
            UiFactory.AddPanel(root, "Background", new Color(0.07f, 0.07f, 0.1f, 1f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var ctx = GameContext.Instance;
            var outcome = ctx?.RunSession?.LastOutcome;
            var kind = outcome?.kind ?? RunResultKind.None;
            var title = kind switch
            {
                RunResultKind.Success => "Victory",
                RunResultKind.Failure => "Defeat",
                RunResultKind.Aborted => "Run Aborted",
                _ => "Run Results"
            };

            var titleColor = kind == RunResultKind.Success
                ? new Color(0.45f, 0.85f, 0.55f)
                : new Color(0.9f, 0.45f, 0.4f);

            UiFactory.AddText(root, "Title", title, 56, TextAnchor.MiddleCenter, titleColor,
                new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.9f), Vector2.zero, Vector2.zero);

            var body = "No outcome recorded.";
            if (outcome != null && ctx != null)
            {
                body =
                    $"{outcome.summary}\n\n" +
                    $"City: {ctx.Content.GetDisplayName(outcome.cityId, outcome.cityId)}\n" +
                    $"Vestiges earned: {outcome.vestigesEarned}\n" +
                    $"Year: {ctx.Progression.Year}  |  Decade: {ctx.Progression.Decade}\n" +
                    $"Arcane Vestiges: {ctx.Progression.ArcaneVestiges}";

                if (kind == RunResultKind.Failure)
                {
                    body += "\n\nCalendar advanced +10 years.";
                }
            }

            UiFactory.AddText(root, "Body", body, 26, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.70f), Vector2.zero, Vector2.zero);

            var hub = UiFactory.AddButton(root, "BackHub", "Return to Hub",
                new Vector2(0.35f, 0.12f), new Vector2(0.65f, 0.22f), Vector2.zero, Vector2.zero,
                new Color(0.2f, 0.35f, 0.5f, 1f));
            hub.onClick.AddListener(() => SceneManager.LoadScene(SceneNames.Hub));
        }
    }
}
