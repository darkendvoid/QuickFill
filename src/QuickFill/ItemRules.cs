using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickFill
{
    internal static class ItemRules
    {
        // Furnace and blast furnace inputs by metal grade, highest first. GoldOre is Petrified tissue (-> Bloodgold).
        private static readonly string[] s_gradeOrder =
        {
            "GoldOre",
            "FlametalOreNew", "FlametalOre",
            "BlackMetalScrap",
            "SilverOre",
            "IronOre", "IronScrap",
            "BronzeScrap",
            "CopperOre", "CopperScrap",
            "TinOre",
        };

        // Shield generator fuels, least needed first. Bone fragments are left over from early-game gear by the time you
        // build a shield generator; Charred bone still goes into Flametal armour, Ashlands weapons and Charred ammo.
        private static readonly string[] s_shieldFuelOrder = { "BoneFragments", "CharredBone" };

        /// <summary>Shield generator fuels, least useful for crafting first; unlisted (modded) fuels follow in the game's order.</summary>
        public static IEnumerable<ItemDrop> ShieldFuelsByPriority(IEnumerable<ItemDrop> fuels)
        {
            return fuels.Where(f => f).OrderBy(f => Rank(s_shieldFuelOrder, f.name));
        }

        /// <summary>Conversions ordered highest grade first; unlisted inputs follow in the game's order (OrderBy is stable).</summary>
        public static IEnumerable<Smelter.ItemConversion> ByGrade(IEnumerable<Smelter.ItemConversion> conversions)
        {
            return conversions.Where(c => c.m_from).OrderBy(c => Rank(s_gradeOrder, c.m_from.name));
        }

        private static int Rank(string[] order, string prefabName)
        {
            int index = Array.IndexOf(order, prefabName);
            return index < 0 ? int.MaxValue : index;
        }
    }
}
