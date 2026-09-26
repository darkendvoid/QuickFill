# Shared paths for QuickFill dev scripts. Source, don't execute.
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VALHEIM_DIR="$HOME/.local/share/Steam/steamapps/common/Valheim"
VALHEIM_APPID=892970
AMETHYST_APPIMAGE="$HOME/Applications/AmethystModManager-x86_64.AppImage"
AMETHYST_GAME_DIR="$HOME/Games/Amethyst/Valheim"
AMETHYST_PROFILE="default"
MOD_NAME="QuickFill"
MOD_DIR="$AMETHYST_GAME_DIR/mods/$MOD_NAME"
MODLIST="$AMETHYST_GAME_DIR/profiles/$AMETHYST_PROFILE/modlist.txt"
BEPINEX_LOG="$VALHEIM_DIR/BepInEx/LogOutput.log"
# Nexus Mods (https://www.nexusmods.com/valheim/mods/4032). The API key lives outside the repo,
# in $NEXUSMODS_API_KEY or ~/.config/nexusmods/api_key.
NEXUS_API="https://api.nexusmods.com/v3"
NEXUS_GAME="valheim"
NEXUS_MOD_ID=4032            # game-scoped ID, as in the mod page URL
NEXUS_FILE_ID=8033184        # the main file; each release is uploaded as a new version of it
