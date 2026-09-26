using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace QuickFill
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    // Client-side only: uses the same RPCs as vanilla interaction, so servers and other players don't need it.
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class QuickFillPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.darkendvoid.quickfill";
        public const string ModName = "QuickFill";
        public const string ModVersion = "0.5.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            QuickFillConfig.Bind(Config);
            PrefabManager.OnVanillaPrefabsAvailable += ReportRecognisedStructures;
            Log.LogInfo($"{ModName} {ModVersion} loaded");
        }

        private void Update()
        {
            Player player = Player.m_localPlayer;
            if (!player || player.IsDead() || InputBlocked() || !QuickFillConfig.Hotkey.Value.IsDown())
                return;
            Filler.FillNearby(player);
        }

        private static bool InputBlocked()
        {
            return (Chat.instance && Chat.instance.HasFocus())
                || global::Console.IsVisible()
                || TextInput.IsVisible()
                || Menu.IsVisible()
                || InventoryGui.IsVisible()
                || StoreGui.IsVisible()
                || Minimap.InTextInput();
        }

        private static bool IsFillable(GameObject go)
        {
            var station = go.GetComponent<CookingStation>();
            return go.GetComponent<Smelter>() || go.GetComponent<Fireplace>() || (station && station.m_useFuel);
        }

        /// <summary>Logs how each fillable vanilla prefab is categorised, so new structures from game updates show up as Other.</summary>
        private void ReportRecognisedStructures()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= ReportRecognisedStructures;

            var counts = new Dictionary<StructureCategory, List<string>>();
            foreach (var obj in PrefabManager.Cache.GetPrefabs(typeof(GameObject)).Values)
            {
                var go = (GameObject)obj;
                if (!go.GetComponent<Piece>() || !IsFillable(go))
                    continue;
                var category = StructureCategories.Classify(go.name);
                if (!counts.TryGetValue(category, out var names))
                    counts[category] = names = new List<string>();
                names.Add(go.name);
            }
            Log.LogInfo("Recognised structures: " + string.Join("; ",
                counts.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}=[{string.Join(",", kv.Value.OrderBy(n => n))}]")));
        }
    }
}
