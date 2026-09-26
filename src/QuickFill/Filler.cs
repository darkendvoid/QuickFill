using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuickFill
{
    /// <summary>
    /// Tops up nearby Smelter-type structures (furnaces, kilns, windmills, spinning wheels...), Fireplaces and fuelled
    /// CookingStations (stone oven)
    /// from the player's inventory and nearby chests, using the same RPCs the vanilla interact switches send.
    /// </summary>
    internal static class Filler
    {
        private static readonly List<Piece> s_pieces = new List<Piece>();

        public static void FillNearby(Player player)
        {
            Vector3 origin = player.transform.position;
            var supply = new Supply(player, QuickFillConfig.UseChests.Value, QuickFillConfig.ChestRange.Value);
            HashSet<string> excluded = QuickFillConfig.GetExcludedItems();

            s_pieces.Clear();
            Piece.GetAllPiecesInRadius(origin, QuickFillConfig.Range.Value, s_pieces);

            var fires = new List<Fireplace>();
            var smelters = new List<Smelter>();
            var ovens = new List<CookingStation>();
            foreach (Piece piece in s_pieces.OrderBy(p => Vector3.Distance(origin, p.transform.position)))
            {
                var category = StructureCategories.Classify(StructureCategories.PrefabName(piece.gameObject));
                if (!QuickFillConfig.IsEnabled(category) || !PrivateArea.CheckAccess(piece.transform.position, 0f, flash: false))
                    continue;
                if (piece.TryGetComponent(out Fireplace fire))
                    fires.Add(fire);
                else if (piece.TryGetComponent(out Smelter smelter))
                    smelters.Add(smelter);
                else if (piece.TryGetComponent(out CookingStation oven) && oven.m_useFuel)
                    ovens.Add(oven);
            }

            // Fires, lights and ovens first: they need little, and share fuel (wood, coal) with kilns and furnaces.
            int filled = 0;
            foreach (Fireplace fire in fires)
                if (FillFireplace(fire, supply, excluded) > 0)
                    filled++;
            foreach (CookingStation oven in ovens)
                if (FillCookingStationFuel(oven, supply, excluded) > 0)
                    filled++;
            foreach (Smelter smelter in smelters)
                if (FillSmelter(smelter, supply, excluded) > 0)
                    filled++;

            Report(player, filled, supply);
        }

        private static int FillFireplace(Fireplace fire, Supply supply, HashSet<string> excluded)
        {
            ZNetView nview = fire.m_nview;
            if (fire.m_infiniteFuel || !fire.m_canRefill || !fire.m_fuelItem || !nview || !nview.IsValid())
                return 0;
            if (excluded.Contains(fire.m_fuelItem.name))
                return 0;

            // Vanilla refuses to add once Ceil(fuel) reaches max; each item adds 1 fuel.
            int space = Mathf.FloorToInt(fire.m_maxFuel) - Mathf.CeilToInt(nview.GetZDO().GetFloat(ZDOVars.s_fuel));
            int count = supply.Take(fire.m_fuelItem, space, out _);
            if (count == 0)
                return 0;

            if (!nview.HasOwner())
                nview.ClaimOwnership();
            nview.InvokeRPC("RPC_AddFuelAmount", (float)count);
            return count;
        }

        private static int FillCookingStationFuel(CookingStation station, Supply supply, HashSet<string> excluded)
        {
            ZNetView nview = station.m_nview;
            if (!station.m_fuelItem || !nview || !nview.IsValid() || excluded.Contains(station.m_fuelItem.name))
                return 0;

            // Like Smelter, the owner's RPC_AddFuel adds 1 per call without a capacity check.
            int space = station.m_maxFuel - Mathf.CeilToInt(station.GetFuel());
            int count = supply.Take(station.m_fuelItem, space, out _);
            for (int i = 0; i < count; i++)
                nview.InvokeRPC("RPC_AddFuel");
            return count;
        }

        private static int FillSmelter(Smelter smelter, Supply supply, HashSet<string> excluded)
        {
            ZNetView nview = smelter.m_nview;
            if (!nview || !nview.IsValid())
                return 0;

            int total = 0;

            // The owner's RPC_AddOre/RPC_AddFuel don't check capacity, so never send more than the free space.
            int oreSpace = smelter.m_maxOre - smelter.GetQueueSize();
            var tried = new HashSet<string>();
            foreach (Smelter.ItemConversion conversion in smelter.m_conversion)
            {
                if (oreSpace <= 0)
                    break;
                if (!conversion.m_from)
                    continue;
                string prefab = conversion.m_from.name;
                if (!tried.Add(prefab) || excluded.Contains(prefab))
                    continue;

                int count = supply.Take(conversion.m_from, oreSpace, out bool cheated);
                for (int i = 0; i < count; i++)
                    nview.InvokeRPC("RPC_AddOre", prefab, cheated);
                oreSpace -= count;
                total += count;
            }

            if (smelter.m_maxFuel > 0 && smelter.m_fuelItem && !excluded.Contains(smelter.m_fuelItem.name))
            {
                int fuelSpace = smelter.m_maxFuel - Mathf.CeilToInt(smelter.GetFuel());
                int count = supply.Take(smelter.m_fuelItem, fuelSpace, out _);
                for (int i = 0; i < count; i++)
                    nview.InvokeRPC("RPC_AddFuel");
                total += count;
            }

            return total;
        }

        private static void Report(Player player, int filled, Supply supply)
        {
            string message;
            if (filled == 0)
            {
                message = "QuickFill: nothing to fill nearby";
            }
            else
            {
                string items = string.Join(", ", supply.Taken.Select(kv => $"{kv.Value} {Localization.instance.Localize(kv.Key)}"));
                message = $"QuickFill: filled {filled} structure{(filled == 1 ? "" : "s")} ({items})";
                if (supply.ContainersUsed > 0)
                    message += $", using {supply.ContainersUsed} chest{(supply.ContainersUsed == 1 ? "" : "s")}";
            }
            player.Message(MessageHud.MessageType.Center, message);
            QuickFillPlugin.Log.LogInfo(message);
        }
    }
}
