#!/usr/bin/env bash
# RealmShards Unity dev-environment helper for Cloud Agents.
# Usage:
#   realmshards-unity.sh activate    # activate license (UNITY_EMAIL/UNITY_PASSWORD or UNITY_LICENSE .ulf)
#   realmshards-unity.sh status      # show current license entitlements
#   realmshards-unity.sh setup       # run RealmShards content setup menus (batchmode)
#   realmshards-unity.sh test        # run EditMode tests (batchmode)
#   realmshards-unity.sh build       # build a Linux64 dev standalone player (throwaway build script)
#   realmshards-unity.sh run         # run the built player headless under Xvfb (software GL)
set -uo pipefail

UNITY=/opt/unity/Editor/Unity
PROJECT=/workspace
LICLIENT="/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client"
LOGDIR="$HOME/unity-logs"
mkdir -p "$LOGDIR"

cmd="${1:-status}"

case "$cmd" in
  activate)
    if [ -n "${UNITY_LICENSE:-}" ]; then
      echo "$UNITY_LICENSE" > "$HOME/unity.ulf"
      "$UNITY" -batchmode -nographics -quit -manualLicenseFile "$HOME/unity.ulf" \
        -logFile "$LOGDIR/activate.log"
    elif [ -n "${UNITY_EMAIL:-}" ] && [ -n "${UNITY_PASSWORD:-}" ]; then
      # Reliable for free Personal accounts: assigns paid seats else falls back to Personal.
      "$LICLIENT" --activate-all --include-personal \
        --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"
    else
      echo "No credentials. Set UNITY_EMAIL+UNITY_PASSWORD or UNITY_LICENSE." >&2
      exit 2
    fi
    echo "activate exit=$?"
    "$LICLIENT" --showEntitlements | head -8
    ;;
  status)
    "$LICLIENT" --showEntitlements | head -12
    ;;
  setup)
    "$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" \
      -executeMethod RealmShards.Editor.RealmShardsFullSetup.RunAll \
      -logFile "$LOGDIR/setup.log"
    echo "setup exit=$?"; tail -5 "$LOGDIR/setup.log"
    ;;
  test)
    "$UNITY" -batchmode -nographics -runTests -projectPath "$PROJECT" \
      -testPlatform EditMode -testResults "$LOGDIR/test-results.xml" \
      -logFile "$LOGDIR/test.log"
    echo "test exit=$?"
    grep -oE '<test-run[^>]*result="[^"]*"[^>]*passed="[^"]*"[^>]*failed="[^"]*"' "$LOGDIR/test-results.xml" | head -1
    ;;
  build)
    # Write a throwaway Linux build entry point, build, then remove it (keeps repo clean).
    BS="$PROJECT/Assets/Game/Scripts/Editor/RealmShardsCloudBuild.cs"
    cat > "$BS" <<'CS'
#if UNITY_EDITOR
using System.Collections.Generic; using System.IO;
using UnityEditor; using UnityEditor.Build.Reporting; using UnityEngine;
public static class RealmShardsCloudBuild {
  public static void BuildLinux() {
    var root = Path.GetDirectoryName(Application.dataPath);
    var outDir = Path.Combine(root, "Builds/Linux"); Directory.CreateDirectory(outDir);
    var exe = Path.Combine(outDir, "RealmShards.x86_64").Replace('\\','/');
    var scenes = new List<string>();
    foreach (var s in EditorBuildSettings.scenes) if (s.enabled && !string.IsNullOrEmpty(s.path)) scenes.Add(s.path);
    var r = BuildPipeline.BuildPlayer(scenes.ToArray(), exe, BuildTarget.StandaloneLinux64, BuildOptions.Development);
    if (r.summary.result != BuildResult.Succeeded) { Debug.LogError("build failed "+r.summary.result); EditorApplication.Exit(1); return; }
    Debug.Log("[RealmShards] Linux build succeeded: "+exe); EditorApplication.Exit(0);
  }
}
#endif
CS
    "$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" \
      -executeMethod RealmShardsCloudBuild.BuildLinux -logFile "$LOGDIR/build.log"
    rc=$?
    rm -f "$BS" "$BS.meta"
    echo "build exit=$rc"; tail -4 "$LOGDIR/build.log"
    ;;
  run)
    pgrep -f "Xvfb :99" >/dev/null || (Xvfb :99 -screen 0 1280x800x24 >/tmp/xvfb.log 2>&1 & sleep 2)
    DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe \
      "$PROJECT/Builds/Linux/RealmShards.x86_64" -logFile "$LOGDIR/run.log" &
    echo "player PID=$! (DISPLAY=:99). Focus window before sending xdotool input — see AGENTS.md."
    ;;
  *)
    echo "unknown command: $cmd" >&2; exit 1;;
esac
