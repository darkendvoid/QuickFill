using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace AutoSmelt
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class AutoSmeltPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.darkendvoid.autosmelt";
        public const string ModName = "AutoSmelt";
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();

            PrefabManager.OnVanillaPrefabsAvailable += OnVanillaPrefabsAvailable;

            Log.LogInfo($"[smoke] {ModName} {ModVersion} loaded (BepInEx OK)");
        }

        private void OnVanillaPrefabsAvailable()
        {
            var smelter = PrefabManager.Instance.GetPrefab("smelter");
            Log.LogInfo($"[smoke] Jotunn OnVanillaPrefabsAvailable fired; smelter prefab found: {smelter != null}");
            PrefabManager.OnVanillaPrefabsAvailable -= OnVanillaPrefabsAvailable;
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    internal static class FejdStartupStartPatch
    {
        // Run ahead of ConfigurationManager's postfix on the same method, which currently throws an NRE.
        [HarmonyPriority(Priority.First)]
        private static void Postfix()
        {
            AutoSmeltPlugin.Log.LogInfo("[smoke] Harmony postfix on FejdStartup.Start hit (main menu reached)");
        }
    }
}
