# AutoSmelt

A Valheim mod built on BepInEx 5, HarmonyX and Jötunn.

## Setup (Linux)

1. Install the .NET SDK (`sudo apt install dotnet-sdk-10.0`). The plugin targets `net462`.
2. Copy `Environment.props.example` to `Environment.props` and adjust the paths to your Valheim install and Amethyst mod folder.
3. Register the mod with Amethyst Mod Manager once: `scripts/register-mod.sh`.

## Dev loop

The Amethyst GUI must be closed while scripts deploy, because it locks the mod library.

| Script | Purpose |
|---|---|
| `scripts/deploy.sh [--no-build] [Debug\|Release]` | Build, rescan the Amethyst library, deploy |
| `scripts/launch.sh` / `scripts/stop.sh` | Start or stop Valheim through Steam (Proton) |
| `scripts/smoketest.sh [timeout] [--keep-running]` | Launch the game and check that the plugin's `[smoke]` markers appear in the log |
| `scripts/log.sh [all\|mod\|errors\|plugins]` | Follow or filter `BepInEx/LogOutput.log` |

VS Code tasks for all of these are in `.vscode/tasks.json`.
