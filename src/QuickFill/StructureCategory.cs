using System.Collections.Generic;

namespace QuickFill
{
    internal enum StructureCategory
    {
        Furnace,
        BlastFurnace,
        Kiln,
        Windmill,
        SpinningWheel,
        Fire,
        Hearth,
        Torch,
        Sconce,
        Brazier,
        Other,
    }

    internal static class StructureCategories
    {
        // Vanilla prefab names, taken from a dump of every Smelter/Fireplace prefab in the game.
        private static readonly Dictionary<string, StructureCategory> s_byPrefab = new Dictionary<string, StructureCategory>
        {
            ["smelter"] = StructureCategory.Furnace,
            ["blastfurnace"] = StructureCategory.BlastFurnace,
            ["charcoal_kiln"] = StructureCategory.Kiln,
            ["windmill"] = StructureCategory.Windmill,
            ["piece_spinningwheel"] = StructureCategory.SpinningWheel,
            ["fire_pit"] = StructureCategory.Fire,
            ["fire_pit_iron"] = StructureCategory.Fire,
            ["bonfire"] = StructureCategory.Fire,
            ["hearth"] = StructureCategory.Hearth,
            ["piece_walltorch"] = StructureCategory.Sconce,
        };

        /// <summary>Anything with a Smelter or Fireplace that isn't listed (eitr refinery, hot tub, modded pieces...) is Other.</summary>
        public static StructureCategory Classify(string prefabName)
        {
            if (s_byPrefab.TryGetValue(prefabName, out var category))
                return category;
            if (prefabName.StartsWith("piece_groundtorch"))
                return StructureCategory.Torch;
            if (prefabName.StartsWith("piece_brazier"))
                return StructureCategory.Brazier;
            return StructureCategory.Other;
        }

        public static string PrefabName(UnityEngine.GameObject go)
        {
            string name = go.name;
            int clone = name.IndexOf("(Clone)", System.StringComparison.Ordinal);
            return clone >= 0 ? name.Substring(0, clone) : name;
        }
    }
}
