#!/usr/bin/env bash
# Launch the game, wait for the plugin's [smoke] markers in the BepInEx log, report, then stop the game.
# Usage: smoketest.sh [timeout_seconds] [--keep-running]
set -uo pipefail
source "$(dirname "$0")/env.sh"

TIMEOUT="${1:-240}"
KEEP=0; [[ "${2:-}" == "--keep-running" ]] && KEEP=1

markers=(
  "$MOD_NAME [0-9.]+ loaded"
  "Recognised structures: .*Furnace=\[smelter\]"
)

start=$(date +%s)
"$(dirname "$0")/launch.sh" || exit 1

echo "Waiting for a fresh log (BepInEx rewrites it at startup)..."
until [[ -f "$BEPINEX_LOG" && $(stat -L -c %Y "$BEPINEX_LOG") -ge $start ]] && grep -q 'Chainloader started' "$BEPINEX_LOG"; do
  (( $(date +%s) - start > TIMEOUT )) && { echo "FAIL: no fresh BepInEx log within ${TIMEOUT}s"; exit 1; }
  sleep 2
done

status=0
for m in "${markers[@]}"; do
  until grep -qE "$m" "$BEPINEX_LOG"; do
    if (( $(date +%s) - start > TIMEOUT )); then status=1; break; fi
    sleep 2
  done
  if grep -qE "$m" "$BEPINEX_LOG"; then echo "PASS: $m"; else echo "FAIL: $m"; fi
done

echo "--- $MOD_NAME log lines ---"
grep -E "$MOD_NAME" "$BEPINEX_LOG"
echo "--- errors mentioning $MOD_NAME ---"
grep -n -A6 -E '^\[(Error|Fatal)' "$BEPINEX_LOG" | grep -B1 -A6 "$MOD_NAME" || echo "none"

(( KEEP )) || "$(dirname "$0")/stop.sh"
exit $status
