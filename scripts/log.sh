#!/usr/bin/env bash
# Inspect the BepInEx log.
#   log.sh            follow the whole log
#   log.sh mod        follow only AutoSmelt lines
#   log.sh errors     print errors/exceptions from the current log (with context)
#   log.sh plugins    print the plugin load summary
set -euo pipefail
source "$(dirname "$0")/env.sh"

case "${1:-all}" in
  all)     tail -n 50 -F "$BEPINEX_LOG" ;;
  mod)     tail -n +1 -F "$BEPINEX_LOG" | grep --line-buffered -E "$MOD_NAME" ;;
  errors)  grep -n -A6 -E '^\[(Error|Fatal)|Exception' "$BEPINEX_LOG" || echo "No errors." ;;
  plugins) grep -E 'BepInEx\] (BepInEx [0-9]|Loading \[|[0-9]+ plugins to load|Chainloader startup)|Could not load|incompatible|dependency' "$BEPINEX_LOG" ;;
  *) echo "usage: $0 [all|mod|errors|plugins]" >&2; exit 2 ;;
esac
