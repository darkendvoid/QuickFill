#!/usr/bin/env bash
# Build a Release zip and upload it to Nexus Mods as a new version of the main file,
# then post that version's CHANGELOG.md section as the mod changelog.
# Usage: nexus-upload.sh [--dry-run] [--no-build]
#   --dry-run   check everything and build the zip, but upload nothing
#   --no-build  zip the existing Release build instead of rebuilding
# API: https://api-docs.nexusmods.com/ (v3 upload flow: create upload -> PUT -> finalise -> new file version)
set -euo pipefail
source "$(dirname "$0")/env.sh"

DRY_RUN=0 BUILD=1
for arg in "$@"; do
  case "$arg" in
    --dry-run) DRY_RUN=1 ;;
    --no-build) BUILD=0 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

die() { echo "ERROR: $*" >&2; exit 1; }

API_KEY="${NEXUSMODS_API_KEY:-}"
[[ -z "$API_KEY" && -r "$HOME/.config/nexusmods/api_key" ]] && API_KEY=$(<"$HOME/.config/nexusmods/api_key")
[[ -n "$API_KEY" ]] || die "No API key: set NEXUSMODS_API_KEY or write it to ~/.config/nexusmods/api_key"

# api METHOD PATH [JSON_BODY] -> response body on stdout; fails on non-2xx with the problem details.
api() {
  local method=$1 path=$2 body=${3:-} out status
  out=$(mktemp)
  status=$(curl -sS -o "$out" -w '%{http_code}' -X "$method" "$NEXUS_API$path" \
    -H "apikey: $API_KEY" -H 'Accept: application/json' \
    ${body:+-H 'Content-Type: application/json' --data "$body"})
  if [[ "$status" != 2* ]]; then
    echo "ERROR: $method $path -> HTTP $status" >&2
    cat "$out" >&2; echo >&2
    rm -f "$out"; return 1
  fi
  cat "$out"; rm -f "$out"
}

# --- Version and changelog checks ---
csproj="$PROJECT_ROOT/src/QuickFill/QuickFill.csproj"
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$csproj")
[[ -n "$version" ]] || die "No <Version> in $csproj"
plugin_version=$(sed -n 's/.*ModVersion = "\(.*\)".*/\1/p' "$PROJECT_ROOT/src/QuickFill/QuickFillPlugin.cs")
[[ "$plugin_version" == "$version" ]] || die "csproj version $version != ModVersion $plugin_version"

# The "## <version>" section of CHANGELOG.md, up to the next heading.
changelog=$(awk -v v="$version" '
  /^## / { if (found) exit; found = ($2 == v); next }
  found' "$PROJECT_ROOT/CHANGELOG.md" | sed '/^[[:space:]]*$/d')
[[ -n "$changelog" ]] || die "CHANGELOG.md has no entries under '## $version'"

git -C "$PROJECT_ROOT" diff --quiet HEAD -- src CHANGELOG.md \
  || echo "WARNING: uncommitted changes in src/ or CHANGELOG.md" >&2

# --- Refuse to upload a version Nexus already has ---
versions=$(api GET "/mod-files/$NEXUS_FILE_ID/versions")
if jq -e --arg v "$version" '.data.versions[] | select(.version == $v)' <<<"$versions" >/dev/null; then
  die "Version $version is already on Nexus (file $NEXUS_FILE_ID). Bump the version first."
fi
previous_id=$(jq -r '.data.versions | max_by(.position | tonumber) | .id // empty' <<<"$versions")
mod_uid=$(api GET "/games/$NEXUS_GAME/mods/$NEXUS_MOD_ID" | jq -r '.data.id')

# --- Build and package ---
if (( BUILD )); then
  dotnet build "$csproj" -c Release -nologo -v minimal
fi
dll="$PROJECT_ROOT/src/QuickFill/bin/Release/QuickFill.dll"
[[ -f "$dll" ]] || die "$dll not found (build with Release first)"
zip_name="QuickFill-$version.zip"
zip_path="$PROJECT_ROOT/$zip_name"
rm -f "$zip_path"
zip -j -q "$zip_path" "$dll"
size=$(stat -c %s "$zip_path")
md5_hex=$(md5sum "$zip_path" | cut -d' ' -f1)
md5_b64=$(openssl dgst -md5 -binary "$zip_path" | base64)

echo "Mod:       https://www.nexusmods.com/$NEXUS_GAME/mods/$NEXUS_MOD_ID (uid $mod_uid)"
echo "File:      $NEXUS_FILE_ID, replacing version id ${previous_id:-none}"
echo "Upload:    $zip_name ($size bytes, md5 $md5_hex)"
echo "Changelog for $version:"
sed 's/^/  /' <<<"$changelog"

if (( DRY_RUN )); then
  echo "Dry run: nothing uploaded."
  exit 0
fi

# --- Upload: create session, PUT to the presigned URL, finalise, wait until available ---
upload=$(api POST /uploads "$(jq -n --arg f "$zip_name" --argjson s "$size" --arg m "$md5_hex" \
  '{filename: $f, size_bytes: $s, md5: $m}')")
upload_id=$(jq -r '.data.id' <<<"$upload")
presigned_url=$(jq -r '.data.presigned_url' <<<"$upload")
echo "Upload session $upload_id created; sending data..."

curl -sS --fail-with-body -X PUT "$presigned_url" --upload-file "$zip_path" \
  -H "Content-Disposition: attachment; filename=\"$zip_name\"" \
  -H "Content-MD5: $md5_b64" >/dev/null

api POST "/uploads/$upload_id/finalise" >/dev/null
for _ in $(seq 60); do
  state=$(api GET "/uploads/$upload_id" | jq -r '.data.state')
  [[ "$state" == available ]] && break
  sleep 5
done
[[ "$state" == available ]] || die "Upload $upload_id still '$state' after 5 minutes"

# --- Attach it as the new version of the main file ---
body=$(jq -n --arg u "$upload_id" --arg v "$version" --arg p "$previous_id" '{
  upload_id: $u,
  name: "QuickFill",
  version: $v,
  file_category: "main",
  primary_mod_manager_download: true,
  allow_mod_manager_download: true,
  update_mod_version: true,
  archive_existing_file: false,
  previous_version_id: (if $p == "" then null else $p end)
}')
created=$(api POST "/mod-files/$NEXUS_FILE_ID/versions" "$body")
echo "Created file version $(jq -r '.data.version.id' <<<"$created")"

api POST "/mods/$mod_uid/changelogs" "$(jq -n --arg v "$version" --arg c "$changelog" \
  '{version: $v, changelog: $c}')" >/dev/null
echo "Changelog posted."
echo "Done: https://www.nexusmods.com/$NEXUS_GAME/mods/$NEXUS_MOD_ID?tab=files"
