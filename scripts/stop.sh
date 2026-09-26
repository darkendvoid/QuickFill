#!/usr/bin/env bash
# Stop a running Valheim (Proton) instance.
set -uo pipefail

pids=$(pgrep -f -i 'valheim.exe')
if [[ -z "$pids" ]]; then
  echo "Valheim is not running."
  exit 0
fi
kill $pids
for _ in $(seq 1 20); do
  pgrep -f -i 'valheim.exe' >/dev/null || { echo "Valheim stopped."; exit 0; }
  sleep 1
done
echo "Still running after 20s; sending SIGKILL." >&2
pkill -9 -f -i 'valheim.exe'
