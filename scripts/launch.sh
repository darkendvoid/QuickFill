#!/usr/bin/env bash
# Launch Valheim through Steam (Proton). Steam must already be running.
set -euo pipefail
source "$(dirname "$0")/env.sh"

if pgrep -f -i 'valheim.exe' >/dev/null; then
  echo "Valheim is already running." >&2
  exit 1
fi
steam -applaunch "$VALHEIM_APPID" >/dev/null 2>&1 &
disown
echo "Launch requested (appid $VALHEIM_APPID)."
