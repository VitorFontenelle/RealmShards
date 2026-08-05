#!/usr/bin/env bash
# Idempotent repository bootstrap for environment builds.
# Runs once during build creation; state is baked into the build snapshot.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
MARKER="$HOME/.cache/realmshards/.build-setup-complete"
LIB_CACHE="$HOME/.cache/realmshards/Library"
LOGDIR="$HOME/unity-logs"
UNITY=/opt/unity/Editor/Unity

mkdir -p "$LOGDIR" "$(dirname "$MARKER")"

if [ ! -x "$UNITY" ]; then
  echo "ERROR: Unity Editor missing at $UNITY — base snapshot must include Unity 6000.5.6f1." >&2
  exit 1
fi
echo "Unity Editor present at $UNITY"
"$UNITY" -version 2>&1 | head -1

install -m 755 "$ROOT/.cursor/scripts/realmshards-unity.sh" "$HOME/realmshards-unity.sh"

# Library/ is gitignored and /workspace is re-cloned on each agent boot.
# Keep the import cache in $HOME so it survives git checkout (baked into build snapshots).
mkdir -p "$LIB_CACHE"
ln -sfn "$LIB_CACHE" "$ROOT/Library"

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
