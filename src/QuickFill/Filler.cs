using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuickFill
{
    /// <summary>
    /// Tops up nearby Smelter-type structures (furnaces, kilns, windmills, spinning wheels...), Fireplaces and fuelled
    /// CookingStations (stone oven, frost foundry) and ShieldGenerators
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
            var cookingStations = new List<CookingStation>();
            var shields = new List<ShieldGenerator>();
            foreach (Piece piece in s_pieces.OrderBy(p => Vector3.Distance(origin, p.transform.position)))
            {
                var category = StructureCategories.Classify(StructureCategories.PrefabName(piece.gameObject));
                if (!QuickFillConfig.IsEnabled(category) || !PrivateArea.CheckAccess(piece.transform.position, 0f, flash: false))
                    continue;
                if (piece.TryGetComponent(out Fireplace fire))
                    fires.Add(fire);
                else if (piece.TryGetComponent(out Smelter smelter))
                    smelters.Add(smelter);
                else if (piece.TryGetComponent(out CookingStation station) && station.m_useFuel)
                    cookingStations.Add(station);
                else if (piece.TryGetComponent(out ShieldGenerator shield))
                    shields.Add(shield);
            }

            // Fires, lights, ovens and foundries first: they need little, and share fuel (wood, coal) with kilns and furnaces.
            int filled = 0;
            var missingFuel = new Dictionary<string, int>();
            foreach (Fireplace fire in fires)
                if (FillFireplace(fire, supply, excluded, missingFuel) > 0)
                    filled++;
            foreach (CookingStation station in cookingStations)
                if (FillCookingStationFuel(station, supply, excluded, missingFuel) > 0)
                    filled++;
            foreach (ShieldGenerator shield in shields)
                if (FillShieldGenerator(shield, supply, excluded, missingFuel) > 0)
                    filled++;
            // Identical smelters (all furnaces, all windmills...) share their inputs; nearest group first.
            foreach (var group in smelters.GroupBy(s => StructureCategories.PrefabName(s.gameObject)))
                filled += FillSmelters(group.ToList(), supply, excluded, missingFuel);

            Report(player, filled, supply, missingFuel);
        }

        private static int FillFireplace(Fireplace fire, Supply supply, HashSet<string> excluded, Dictionary<string, int> missingFuel)
        {
            ZNetView nview = fire.m_nview;
            if (fire.m_infiniteFuel || !fire.m_canRefill || !fire.m_fuelItem || !nview || !nview.IsValid())
                return 0;
            if (excluded.Contains(fire.m_fuelItem.name))
                return 0;

            // Vanilla refuses to add once Ceil(fuel) reaches max; each item adds 1 fuel.
            int space = Mathf.FloorToInt(fire.m_maxFuel) - Mathf.CeilToInt(nview.GetZDO().GetFloat(ZDOVars.s_fuel));
            int count = TakeFuel(supply, fire.m_fuelItem, space, missingFuel);
            if (count == 0)
                return 0;

            if (!nview.HasOwner())
                nview.ClaimOwnership();
            nview.InvokeRPC("RPC_AddFuelAmount", (float)count);
            return count;
        }

        private static int FillCookingStationFuel(CookingStation station, Supply supply, HashSet<string> excluded, Dictionary<string, int> missingFuel)
        {
            ZNetView nview = station.m_nview;
            if (!station.m_fuelItem || !nview || !nview.IsValid() || excluded.Contains(station.m_fuelItem.name))
                return 0;

            // Like Smelter, the owner's RPC_AddFuel adds 1 per call without a capacity check.
            int space = station.m_maxFuel - Mathf.CeilToInt(station.GetFuel());
            int count = TakeFuel(supply, station.m_fuelItem, space, missingFuel);
            for (int i = 0; i < count; i++)
                nview.InvokeRPC("RPC_AddFuel");
            return count;
        }

        private static int FillShieldGenerator(ShieldGenerator shield, Supply supply, HashSet<string> excluded, Dictionary<string, int> missingFuel)
        {
            ZNetView nview = shield.m_nview;
            if (!nview || !nview.IsValid())
                return 0;
            List<ItemDrop> fuels = ItemRules.ShieldFuelsByPriority(shield.m_fuelItems).Where(f => !excluded.Contains(f.name)).ToList();
            if (fuels.Count == 0)
                return 0;

            // Vanilla refuses once fuel > max - 1; the owner's RPC_AddFuel adds 1 per call without a capacity check.
            int space = shield.m_maxFuel - Mathf.CeilToInt(shield.GetFuel());
            int count = 0;
            foreach (ItemDrop fuel in fuels)
                count += supply.Take(fuel, space - count, out _);
            if (count < space)
            {
                // Reported as the preferred fuel (Bone fragments).
                string name = fuels[0].m_itemData.m_shared.m_name;
                missingFuel[name] = (missingFuel.TryGetValue(name, out int previous) ? previous : 0) + space - count;
            }

            if (count > 0 && !nview.HasOwner())
                nview.ClaimOwnership();
            for (int i = 0; i < count; i++)
                nview.InvokeRPC("RPC_AddFuel");
            return count;
        }

        /// <summary>Splits the inputs between the stations (see <see cref="SmelterPlan"/>) and returns how many were filled.</summary>
        private static int FillSmelters(List<Smelter> smelters, Supply supply, HashSet<string> excluded, Dictionary<string, int> missingFuel)
        {
            smelters = smelters.Where(s => s.m_nview && s.m_nview.IsValid()).ToList();
            if (smelters.Count == 0)
                return 0;

            // All stations in a group are the same prefab, so they share conversions and fuel.
            Smelter first = smelters[0];
            var inputs = new List<ItemDrop>();
            var seen = new HashSet<string>();
            foreach (Smelter.ItemConversion conversion in ItemRules.ByGrade(first.m_conversion))
                if (seen.Add(conversion.m_from.name) && !excluded.Contains(conversion.m_from.name))
                    inputs.Add(conversion.m_from);

            ItemDrop fuel = first.m_maxFuel > 0 && first.m_fuelItem && !excluded.Contains(first.m_fuelItem.name) ? first.m_fuelItem : null;
            var states = smelters.Select(s => new StationState
            {
                // The owner's RPC_AddOre/RPC_AddFuel don't check capacity, so never send more than the free space.
                Queued = s.GetQueueSize(),
                OreSpace = s.m_maxOre - s.GetQueueSize(),
                Fuel = fuel ? s.GetFuel() : 0f,
                FuelSpace = fuel ? s.m_maxFuel - Mathf.CeilToInt(s.GetFuel()) : 0,
                FuelPerProduct = fuel ? s.m_fuelPerProduct : 0,
            }).ToList();
            SmelterPlan.Plan(states, inputs.Select(supply.Count).ToList(), fuel ? supply.Count(fuel) : 0);

            var added = new int[smelters.Count];
            for (int i = 0; i < smelters.Count; i++)
            {
                ZNetView nview = smelters[i].m_nview;
                foreach (var byInput in states[i].Units.GroupBy(u => u))
                {
                    ItemDrop input = inputs[byInput.Key];
                    int count = supply.Take(input, byInput.Count(), out bool cheated);
                    for (int n = 0; n < count; n++)
                        nview.InvokeRPC("RPC_AddOre", input.name, cheated);
                    added[i] += count;
                }
            }

            if (fuel)
            {
                // Fuel for the planned ore first, then top every station up with whatever is left.
                var fuelAdded = new int[smelters.Count];
                for (int i = 0; i < smelters.Count; i++)
                    fuelAdded[i] = supply.Take(fuel, states[i].PlannedFuel, out _);
                for (int i = 0; i < smelters.Count; i++)
                {
                    fuelAdded[i] += TakeFuel(supply, fuel, states[i].FuelSpace - fuelAdded[i], missingFuel);
                    for (int n = 0; n < fuelAdded[i]; n++)
                        smelters[i].m_nview.InvokeRPC("RPC_AddFuel");
                    added[i] += fuelAdded[i];
                }
            }

            return added.Count(n => n > 0);
        }

        /// <summary>Takes fuel for one structure and records any shortfall.</summary>
        private static int TakeFuel(Supply supply, ItemDrop fuel, int space, Dictionary<string, int> missingFuel)
        {
            if (space <= 0)
                return 0;
            int count = supply.Take(fuel, space, out _);
            if (count < space)
            {
                string name = fuel.m_itemData.m_shared.m_name;
                missingFuel[name] = (missingFuel.TryGetValue(name, out int previous) ? previous : 0) + space - count;
            }
            return count;
        }

        private static void Report(Player player, int filled, Supply supply, Dictionary<string, int> missingFuel)
        {
            string message;
            if (filled == 0)
            {
                message = missingFuel.Count == 0 ? "QuickFill: nothing to fill nearby" : "QuickFill: nothing filled";
            }
            else
            {
                message = $"QuickFill: filled {filled} structure{(filled == 1 ? "" : "s")} ({Describe(supply.Taken)})";
                if (supply.ContainersUsed > 0)
                    message += $", using {supply.ContainersUsed} chest{(supply.ContainersUsed == 1 ? "" : "s")}";
            }
            if (missingFuel.Count > 0)
                message += $"\nMissing fuel: {Describe(missingFuel)}";

            player.Message(MessageHud.MessageType.Center, message);
            QuickFillPlugin.Log.LogInfo(message.Replace("\n", " | "));
        }

        private static string Describe(Dictionary<string, int> items)
        {
            return string.Join(", ", items.Select(kv => $"{kv.Value} {Localization.instance.Localize(kv.Key)}"));
        }
    }
}
