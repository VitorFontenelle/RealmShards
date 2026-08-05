#!/usr/bin/env bash
# Per-pod initialization (runs on every agent boot; must be fast and idempotent).
set -euo pipefail

mkdir -p "$HOME/unity-logs"

if [ -f /workspace/.cursor/scripts/realmshards-unity.sh ]; then
  install -m 755 /workspace/.cursor/scripts/realmshards-unity.sh "$HOME/realmshards-unity.sh"
fi

# License activation is intentionally on-demand — see AGENTS.md and ~/realmshards-unity.sh activate.
