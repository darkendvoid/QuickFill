"""Rescan Amethyst's mod library for a profile (the GUI's Refresh), so new/changed mod folders get deployed.

Run with the AppImage's bundled interpreter; see deploy.sh.
Usage: amethyst-refresh.py <game> <profile>
"""
import os
import sys

src = os.path.join(os.environ["APPDIR"], "share", "amethyst-mod-manager")
sys.path.insert(0, src)
os.chdir(src)

import cli  # noqa: E402

cli._setup_path()

from Utils.games.discovery import discover_games  # noqa: E402
from Utils.filegraph.service import FileGraphService  # noqa: E402

game_key, profile = sys.argv[1], sys.argv[2]
game = cli._find_game(discover_games(), game_key)
if game is None:
    sys.exit(f"game '{game_key}' not found")
profile_dir = game.get_profile_root() / "profiles" / profile
FileGraphService.open_library(game, profile_dir, log_fn=print).refresh(profile_dir)
print(f"Refreshed Amethyst library: {game.name} / {profile}")
