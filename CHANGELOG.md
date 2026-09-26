# Changelog

## 0.6.0
- Frigid kilns are supported: QuickFill fills them with Ice, which they turn into Liquid Frost. There's a new "Frigid kilns" toggle, on by default.
- Petrified tissue can be loaded into blast furnaces again and is loaded first, as the highest-grade material. 0.5.0 to 0.5.2 blocked it by mistake as "Gold ore", its internal item name.

## 0.5.2
- Oat and Oat seeds are supported again as windmill inputs. They are new Deep North items, not dev items. Gold ore stays blocked.
- Add `OatSeeds` to Excluded items if you want to keep seeds for planting.

## 0.5.1
- Oat is never used either; like Oat seeds, it is an unused dev item. Windmills now only take barley.

## 0.5.0
- Furnaces and blast furnaces load the highest-grade material first: Flametal, Black metal scrap, Silver, Iron, Bronze scrap, Copper, Tin.
- The on-screen message lists missing fuel when a structure could take more than you have, e.g. "Missing fuel: 12 Resin, 20 Coal".
- Unused dev items (Gold ore, Oat seeds) are never used. OatSeeds is removed from the default excluded items.

## 0.4.0
- First public release.
- Press F7 to fill nearby furnaces, blast furnaces, kilns, windmills, spinning wheels, eitr refineries, hot tubs, stone ovens, fires, hearths, torches, sconces and braziers.
- Items come from your inventory, then nearby chests.
- Configurable hotkey, fill range and chest range (1–250 m), excluded items and per-structure toggles.
