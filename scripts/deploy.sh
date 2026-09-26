#!/usr/bin/env bash
# Build the plugin into the Amethyst mod folder, then run an Amethyst deploy.
# Usage: deploy.sh [--no-build] [Debug|Release]
set -euo pipefail
source "$(dirname "$0")/env.sh"

BUILD=1
[[ "${1:-}" == "--no-build" ]] && { BUILD=0; shift; }
CONFIG="${1:-Debug}"

if pgrep -f -i 'valheim.exe' >/dev/null; then
  echo "Valheim is running; stop it first (scripts/stop.sh)." >&2
  exit 1
fi

if (( BUILD )); then
  dotnet build "$PROJECT_ROOT/src/AutoSmelt/AutoSmelt.csproj" -c "$CONFIG" -nologo -v minimal
fi

# Amethyst deploys from a cached file catalog; rescan so new/changed mod files are picked up.
# Both steps fail if the Amethyst GUI is open (it holds the library lock).
"$AMETHYST_APPIMAGE" python3 "$PROJECT_ROOT/scripts/amethyst-refresh.py" Valheim "$AMETHYST_PROFILE" 2>&1 | grep -v '^\[startup\]'
"$AMETHYST_APPIMAGE" deploy Valheim "$AMETHYST_PROFILE"

target="$VALHEIM_DIR/BepInEx/plugins/$MOD_NAME.dll"
if [[ -e "$target" ]]; then
  echo "Deployed: $target -> $(readlink -f "$target")"
else
  echo "WARNING: $target not present after deploy" >&2
  exit 1
fi
