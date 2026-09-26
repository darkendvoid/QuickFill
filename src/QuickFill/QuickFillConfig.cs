using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace QuickFill
{
    internal static class QuickFillConfig
    {
        private const string General = "1 - General";
        private const string Structures = "2 - Structures";

        public static ConfigEntry<KeyboardShortcut> Hotkey;
        public static ConfigEntry<float> Range;
        public static ConfigEntry<string> ExcludedItems;
        public static ConfigEntry<bool> UseChests;
        public static ConfigEntry<float> ChestRange;

        private static readonly Dictionary<StructureCategory, ConfigEntry<bool>> s_enabled = new Dictionary<StructureCategory, ConfigEntry<bool>>();

        public static void Bind(ConfigFile config)
        {
            Hotkey = config.Bind(General, "Fill hotkey", new KeyboardShortcut(KeyCode.F7),
                Describe("Key that fills every enabled structure in range.", order: 6));
            Range = config.Bind(General, "Range (meters)", 25f,
                Describe("How far from you structures are filled. Only loaded areas count (roughly 100-150 m around you).", order: 5, range: new AcceptableValueRange<float>(1f, 250f)));
            UseChests = config.Bind(General, "Take from chests", true,
                Describe("Also take items from nearby player-built chests you can open (after your own inventory, nearest chest first). Chests someone has open are skipped.", order: 4));
            ChestRange = config.Bind(General, "Chest range (meters)", 25f,
                Describe("How far from you chests are used as a source.", order: 3, range: new AcceptableValueRange<float>(1f, 250f)));
            ExcludedItems = config.Bind(General, "Excluded items", "FineWood, RoundLog",
                Describe("Comma-separated item prefab names never used for filling (e.g. FineWood and RoundLog keep Fine wood and Core wood out of kilns).", order: 1));

            int order = 100;
            BindCategory(config, StructureCategory.Furnace, "Furnaces", "Smelters: ore and coal.", true, order--);
            BindCategory(config, StructureCategory.BlastFurnace, "Blast furnaces", "Blast furnaces: ore/scrap and coal.", true, order--);
            BindCategory(config, StructureCategory.Kiln, "Kilns", "Charcoal kilns: wood.", true, order--);
            BindCategory(config, StructureCategory.Windmill, "Windmills", "Windmills: barley and oats.", true, order--);
            BindCategory(config, StructureCategory.SpinningWheel, "Spinning wheels", "Spinning wheels: flax.", true, order--);
            BindCategory(config, StructureCategory.EitrRefinery, "Eitr refineries", "Eitr refineries: soft tissue and sap.", true, order--);
            BindCategory(config, StructureCategory.HotTub, "Hot tubs", "Hot tubs: wood.", true, order--);
            BindCategory(config, StructureCategory.Oven, "Stone ovens", "Stone ovens: wood (fuel only, never food).", true, order--);
            BindCategory(config, StructureCategory.Fire, "Fires", "Campfires, iron fire pits and bonfires: wood.", true, order--);
            BindCategory(config, StructureCategory.Hearth, "Hearths", "Hearths: wood.", true, order--);
            BindCategory(config, StructureCategory.Torch, "Torches", "Standing torches: resin, guck or greydwarf eyes.", true, order--);
            BindCategory(config, StructureCategory.Sconce, "Sconces", "Wall sconces: resin.", true, order--);
            BindCategory(config, StructureCategory.Brazier, "Braziers", "Standing and hanging braziers: coal or greydwarf eyes.", true, order--);
            BindCategory(config, StructureCategory.Other, "Other", "Everything else that takes fuel or input: frost kiln, frost foundry, jack-o-turnip, snow lantern and modded structures.", false, order--);
        }

        public static bool IsEnabled(StructureCategory category) => s_enabled[category].Value;

        public static HashSet<string> GetExcludedItems()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in ExcludedItems.Value.Split(','))
            {
                string name = part.Trim();
                if (name.Length > 0)
                    set.Add(name);
            }
            return set;
        }

        private static void BindCategory(ConfigFile config, StructureCategory category, string key, string description, bool defaultValue, int order)
        {
            s_enabled[category] = config.Bind(Structures, key, defaultValue, Describe(description, order));
        }

        private static ConfigDescription Describe(string description, int order, AcceptableValueBase range = null)
        {
            return new ConfigDescription(description, range, new ConfigurationManagerAttributes { Order = order });
        }

        // Read by name via reflection by Configuration Manager; only the fields used here are declared.
#pragma warning disable 0649
        private sealed class ConfigurationManagerAttributes
        {
            public int? Order;
        }
#pragma warning restore 0649
    }
}
