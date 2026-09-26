# QuickFill

A Valheim mod built on BepInEx, HarmonyX and Jötunn.

## Description

QuickFill saves you from walking to every furnace, kiln and torch in your base and feeding it one item at a time. Press F7 and every enabled structure within range is topped up in one go, using items from your inventory and nearby chests. It uses the same actions as the vanilla interact button, so it works on dedicated multiplayer servers and the server doesn't need the mod.

## Installation

To install this mod, the easiest way is to just use Vortex, the Nexus Mods mod manager. It should take care of all dependencies.

To install manually, download `QuickFill-<version>.zip` from [Releases](https://github.com/darkendvoid/QuickFill/releases) and place `QuickFill.dll` in the `BepInEx/plugins` folder. You will need BepInEx and Jotunn.

## Main features

- **One-key filling:** press F7 (rebindable) to fill every enabled structure around you.
- **Supported structures:**
  - **Processing:** furnaces, blast furnaces, charcoal kilns, windmills, spinning wheels, eitr refineries.
  - **Fuel:** hot tubs, stone ovens (fuel only, never food).
  - **Fires and lights:** campfires, iron fire pits, bonfires, hearths, standing torches, sconces, braziers.
- **Pulls from chests:** your own inventory is used first, then nearby player-built chests from nearest to farthest.
  - Chests that someone else has open, private chests you don't own and chests inside wards you can't access are skipped.
  - QuickFill takes ownership of a chest before taking from it, so items are never duplicated or lost in multiplayer.
- **Never overfills:** each structure gets exactly enough to fill it, and anything already full is left alone.
- **Shares fuel sensibly:** fires, lights and ovens are filled before kilns and furnaces, so the kiln doesn't eat all your wood.
- **Keeps valuable items safe:** an excluded-items list keeps Fine Wood and Core Wood out of kilns and Oat Seeds out of windmills by default, and you can add your own.
- **Respects wards:** structures inside wards you don't have access to are skipped.
- **Clear feedback:** an on-screen summary shows what was used, e.g. "QuickFill: filled 11 structures (26 Wood, 2 Flax, 3 Barley), using 3 chests".
- **Fully configurable in Configuration Manager:**
  - hotkey;
  - fill range, 1–250 m, default 25;
  - chest range, 1–250 m, default 25;
  - chest use on/off;
  - excluded items;
  - an on/off switch for each structure type;
  - an "Other" category (frost kiln, frost foundry, jack-o-turnip, snow lantern, modded structures), off by default.

## Requirements

- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)
- [Jotunn, the Valheim Library](https://www.nexusmods.com/valheim/mods/1138)
- Configuration Manager (optional) to change settings in game with F1. You can also edit `BepInEx/config/com.darkendvoid.quickfill.cfg` by hand.
- **Multiplayer:** only needs to be installed on the client. The server and other players don't need it.
- **Range limit:** Valheim only loads buildings within roughly 100–150 m of you, so structures and chests further away won't be reached even at the maximum range setting.

## Shout outs

Thanks to the BepInEx team for BepInEx and HarmonyX, to the Valheim Modding team for Jotunn, and to the Configuration Manager authors for making in-game settings easy.

## Development

### Setup (Linux)

1. Install the .NET SDK (`sudo apt install dotnet-sdk-10.0`). The plugin targets `net462`.
2. Copy `Environment.props.example` to `Environment.props` and adjust the paths to your Valheim install and Amethyst mod folder.
3. Register the mod with Amethyst Mod Manager once: `scripts/register-mod.sh`.

### Dev loop

The Amethyst GUI must be closed while scripts deploy, because it locks the mod library.

| Script | Purpose |
|---|---|
| `scripts/deploy.sh [--no-build] [Debug\|Release]` | Build, rescan the Amethyst library, deploy |
| `scripts/launch.sh` / `scripts/stop.sh` | Start or stop Valheim through Steam (Proton) |
| `scripts/smoketest.sh [timeout] [--keep-running]` | Launch the game and check the log that QuickFill loaded and recognised the vanilla structures |
| `scripts/log.sh [all\|mod\|errors\|plugins]` | Follow or filter `BepInEx/LogOutput.log` |

VS Code tasks for all of these are in `.vscode/tasks.json`.
