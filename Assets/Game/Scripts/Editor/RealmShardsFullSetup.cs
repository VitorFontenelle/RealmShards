#if UNITY_EDITOR
using System;
using System.IO;
using RealmShards.EditorTools;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RealmShards.Editor
{
    /// <summary>
    /// Batchmode entry points for full content setup and optional Windows player build.
    /// </summary>
    public static class RealmShardsFullSetup
    {
        private const string WindowsBuildDir = "Builds/Windows";
        private const string WindowsExeName = "RealmShards.exe";

        /// <summary>
        /// Runs all RealmShards setup menus in the documented order.
        /// Invoke: -executeMethod RealmShards.Editor.RealmShardsFullSetup.RunAll
        /// </summary>
        public static void RunAll()
        {
            try
            {
                Debug.Log("[RealmShards] FullSetup: Setup Player Content...");
                RealmShardsContentBuilder.BuildAllMenu();

                Debug.Log("[RealmShards] FullSetup: Setup CityRun Stage 2...");
                CityRunStage2Setup.RunSetup();

                Debug.Log("[RealmShards] FullSetup: Setup Items Content...");
                RealmShardsItemContentBuilder.BuildItemsMenu();

                Debug.Log("[RealmShards] FullSetup: Setup Magic Schools...");
                RealmShardsMagicContentBuilder.BuildMenu();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[RealmShards] FullSetup: all four setups completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[RealmShards] FullSetup FAILED: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        /// <summary>
        /// Builds a Windows 64-bit standalone under Builds/Windows/.
        /// Invoke: -executeMethod RealmShards.Editor.RealmShardsFullSetup.BuildWindowsPlayer
        /// </summary>
        public static void BuildWindowsPlayer()
        {
            try
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                var outDir = Path.Combine(projectRoot, WindowsBuildDir);
                Directory.CreateDirectory(outDir);
                var exePath = Path.Combine(outDir, WindowsExeName).Replace('\\', '/');

                var scenes = EditorBuildSettings.scenes;
                var enabled = new System.Collections.Generic.List<string>();
                foreach (var s in scenes)
                {
                    if (s.enabled && !string.IsNullOrEmpty(s.path))
                    {
                        enabled.Add(s.path);
                    }
                }

                if (enabled.Count == 0)
                {
                    throw new InvalidOperationException("No enabled scenes in EditorBuildSettings.");
                }

                Debug.Log("[RealmShards] Building Windows player → " + exePath);
                var report = BuildPipeline.BuildPlayer(
                    enabled.ToArray(),
                    exePath,
                    BuildTarget.StandaloneWindows64,
                    BuildOptions.None);

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Build failed: " + report.summary.result + " errors=" + report.summary.totalErrors);
                }

                Debug.Log("[RealmShards] Windows player build succeeded: " + exePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("[RealmShards] BuildWindowsPlayer FAILED: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif
