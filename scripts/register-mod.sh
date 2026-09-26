#!/usr/bin/env bash
# One-time: create the Amethyst mod folder + meta.ini and enable it in the profile.
set -euo pipefail
source "$(dirname "$0")/env.sh"

mkdir -p "$MOD_DIR"
if [[ ! -f "$MOD_DIR/meta.ini" ]]; then
  cat > "$MOD_DIR/meta.ini" <<EOF
[General]
gamename = valheim
version = 0.1.0
author = darkendvoid
nexusname = $MOD_NAME
description = Local development build of $MOD_NAME.
rootfolder = false
installed = $(date +%Y-%m-%dT%H:%M:%S)
EOF
  echo "Created $MOD_DIR/meta.ini"
fi

if grep -qxE "[+-]$MOD_NAME" "$MODLIST"; then
  sed -i "s/^-$MOD_NAME\$/+$MOD_NAME/" "$MODLIST"
  echo "$MOD_NAME already in modlist (ensured enabled)"
else
  cp "$MODLIST" "$MODLIST.bak.$(date +%s)"
  # Top of modlist.txt = highest priority.
  sed -i "1i +$MOD_NAME" "$MODLIST"
  echo "Added +$MOD_NAME to $MODLIST"
fi
