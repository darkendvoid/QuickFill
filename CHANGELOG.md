# Changelog

## 0.9.0
- Shield generators are supported, with a new "Shield generators" toggle, on by default. QuickFill tops them up to 10 with Bone fragments first, and uses Charred bone only when you run out, since Flametal armour, Ashlands weapons and Charred arrows and bolts still need it. Add `CharredBone` to Excluded items to never use it.

## 0.8.0
- Ore and other inputs are split evenly between stations of the same type instead of filling the nearest one first, e.g. 6 Silver across 3 furnaces is loaded 2/2/2. Applies to furnaces, blast furnaces, windmills, kilns, spinning wheels, eitr refineries and frigid kilns.
- Furnaces, blast furnaces and eitr refineries get the fuel their ore needs first, then any fuel left over tops them up.
- When there isn't enough fuel for an even split, ore goes where it can be smelted with the least fuel, even if that is a single furnace: stations that already have fuel are used first, and ore beyond what the fuel covers goes into fuelled stations, not cold ones.

## 0.7.0
- Frost foundries are supported: QuickFill fuels them with Liquid Frost, and never touches their casting slots. There's a new "Frost foundries" toggle, on by default.
- Pair with frigid kilns (0.6.0), which turn Ice into Liquid Frost.

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
