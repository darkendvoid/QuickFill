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

        /// <summary>Conversions ordered highest grade first; unlisted inputs follow in the game's order (OrderBy is stable).</summary>
        public static IEnumerable<Smelter.ItemConversion> ByGrade(IEnumerable<Smelter.ItemConversion> conversions)
        {
            return conversions.Where(c => c.m_from).OrderBy(c => Rank(c.m_from.name));
        }

        private static int Rank(string prefabName)
        {
            int index = Array.IndexOf(s_gradeOrder, prefabName);
            return index < 0 ? int.MaxValue : index;
        }
    }
}
