using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickFill
{
    internal static class ItemRules
    {
        // Dev/unused items the game still lists as valid inputs; never fill with them.
        private static readonly HashSet<string> s_unsupported = new HashSet<string> { "GoldOre" };

        // Furnace and blast furnace inputs by metal grade, highest first.
        private static readonly string[] s_gradeOrder =
        {
            "FlametalOreNew", "FlametalOre",
            "BlackMetalScrap",
            "SilverOre",
            "IronOre", "IronScrap",
            "BronzeScrap",
            "CopperOre", "CopperScrap",
            "TinOre",
        };

        public static bool IsUnsupported(string prefabName) => s_unsupported.Contains(prefabName);

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
