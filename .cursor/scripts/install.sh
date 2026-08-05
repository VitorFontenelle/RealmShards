#!/usr/bin/env bash
# Idempotent repository bootstrap for environment builds.
# Runs once during build creation; state is baked into the build snapshot.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MARKER="$ROOT/.cursor/.build-setup-complete"
LOGDIR="$HOME/unity-logs"
UNITY=/opt/unity/Editor/Unity

mkdir -p "$LOGDIR"

if [ ! -x "$UNITY" ]; then
  echo "ERROR: Unity Editor missing at $UNITY — base snapshot must include Unity 6000.5.6f1." >&2
  exit 1
fi
echo "Unity Editor present at $UNITY"
"$UNITY" -version 2>&1 | head -1

install -m 755 "$ROOT/.cursor/scripts/realmshards-unity.sh" "$HOME/realmshards-unity.sh"

if [ -f "$MARKER" ]; then
  echo "Build setup marker present ($MARKER); skipping Unity content setup and tests."
  exit 0
fi

echo "Running RealmShards content setup (batchmode)..."
if ! "$HOME/realmshards-unity.sh" setup; then
  echo "ERROR: RealmShards content setup failed. See $LOGDIR/setup.log" >&2
  exit 1
fi

echo "Running EditMode tests..."
if ! "$HOME/realmshards-unity.sh" test; then
  echo "ERROR: EditMode tests failed. See $LOGDIR/test.log and $LOGDIR/test-results.xml" >&2
  exit 1
fi

touch "$MARKER"
echo "Build setup complete."
