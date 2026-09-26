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
