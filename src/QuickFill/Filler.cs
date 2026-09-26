using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuickFill
{
    /// <summary>
    /// Tops up nearby Smelter-type structures (furnaces, kilns, windmills, spinning wheels...) and Fireplaces
    /// from the player's inventory, using the same RPCs the vanilla interact switches send.
    /// </summary>
    internal static class Filler
    {
        private static readonly List<Piece> s_pieces = new List<Piece>();

        public static void FillNearby(Player player)
        {
            Vector3 origin = player.transform.position;
            Inventory inventory = player.GetInventory();
            HashSet<string> excluded = QuickFillConfig.GetExcludedItems();

            s_pieces.Clear();
            Piece.GetAllPiecesInRadius(origin, QuickFillConfig.Range.Value, s_pieces);

            var fires = new List<Fireplace>();
            var smelters = new List<Smelter>();
            foreach (Piece piece in s_pieces.OrderBy(p => Vector3.Distance(origin, p.transform.position)))
            {
                var category = StructureCategories.Classify(StructureCategories.PrefabName(piece.gameObject));
                if (!QuickFillConfig.IsEnabled(category) || !PrivateArea.CheckAccess(piece.transform.position, 0f, flash: false))
                    continue;
                if (piece.TryGetComponent(out Fireplace fire))
                    fires.Add(fire);
                else if (piece.TryGetComponent(out Smelter smelter))
                    smelters.Add(smelter);
            }

            // Fires and lights first: they need little, and share fuel (wood, coal) with kilns and furnaces.
            var added = new Dictionary<string, int>();
            int filled = 0;
            foreach (Fireplace fire in fires)
                if (FillFireplace(fire, inventory, excluded, added) > 0)
                    filled++;
            foreach (Smelter smelter in smelters)
                if (FillSmelter(smelter, inventory, excluded, added) > 0)
                    filled++;

            Report(player, filled, added);
        }

        private static int FillFireplace(Fireplace fire, Inventory inventory, HashSet<string> excluded, Dictionary<string, int> added)
        {
            ZNetView nview = fire.m_nview;
            if (fire.m_infiniteFuel || !fire.m_canRefill || !fire.m_fuelItem || !nview || !nview.IsValid())
                return 0;
            if (excluded.Contains(fire.m_fuelItem.name))
                return 0;

            // Vanilla refuses to add once Ceil(fuel) reaches max; each item adds 1 fuel.
            int space = Mathf.FloorToInt(fire.m_maxFuel) - Mathf.CeilToInt(nview.GetZDO().GetFloat(ZDOVars.s_fuel));
            int count = Take(inventory, fire.m_fuelItem, space, added, out _);
            if (count == 0)
                return 0;

            if (!nview.HasOwner())
                nview.ClaimOwnership();
            nview.InvokeRPC("RPC_AddFuelAmount", (float)count);
            return count;
        }

        private static int FillSmelter(Smelter smelter, Inventory inventory, HashSet<string> excluded, Dictionary<string, int> added)
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

                int count = Take(inventory, conversion.m_from, oreSpace, added, out bool cheated);
                for (int i = 0; i < count; i++)
                    nview.InvokeRPC("RPC_AddOre", prefab, cheated);
                oreSpace -= count;
                total += count;
            }

            if (smelter.m_maxFuel > 0 && smelter.m_fuelItem && !excluded.Contains(smelter.m_fuelItem.name))
            {
                int fuelSpace = smelter.m_maxFuel - Mathf.CeilToInt(smelter.GetFuel());
                int count = Take(inventory, smelter.m_fuelItem, fuelSpace, added, out _);
                for (int i = 0; i < count; i++)
                    nview.InvokeRPC("RPC_AddFuel");
                total += count;
            }

            return total;
        }

        /// <summary>Removes up to <paramref name="wanted"/> of an item from the inventory and returns how many were taken.</summary>
        private static int Take(Inventory inventory, ItemDrop item, int wanted, Dictionary<string, int> added, out bool cheated)
        {
            cheated = false;
            if (wanted <= 0)
                return 0;

            string name = item.m_itemData.m_shared.m_name;
            int count = Math.Min(wanted, inventory.CountItems(name));
            if (count <= 0)
                return 0;

            cheated = inventory.GetAllItems().Any(i => i.m_shared.m_name == name && i.m_cheated);
            inventory.RemoveItem(name, count);
            added[name] = added.TryGetValue(name, out int previous) ? previous + count : count;
            return count;
        }

        private static void Report(Player player, int filled, Dictionary<string, int> added)
        {
            string message;
            if (filled == 0)
            {
                message = "QuickFill: nothing to fill nearby";
            }
            else
            {
                string items = string.Join(", ", added.Select(kv => $"{kv.Value} {Localization.instance.Localize(kv.Key)}"));
                message = $"QuickFill: filled {filled} structure{(filled == 1 ? "" : "s")} ({items})";
            }
            player.Message(MessageHud.MessageType.Center, message);
            QuickFillPlugin.Log.LogInfo(message);
        }
    }
}
