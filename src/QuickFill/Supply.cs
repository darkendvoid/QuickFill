using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuickFill
{
    /// <summary>
    /// Items available for filling: the player's inventory first, then nearby chests, nearest first.
    /// </summary>
    internal class Supply
    {
        private readonly List<Inventory> _inventories = new List<Inventory>();
        private readonly Dictionary<Inventory, Container> _containers = new Dictionary<Inventory, Container>();
        private readonly HashSet<Container> _usedContainers = new HashSet<Container>();
        private static readonly List<Piece> s_pieces = new List<Piece>();

        public readonly Dictionary<string, int> Taken = new Dictionary<string, int>();
        public int ContainersUsed => _usedContainers.Count;

        public Supply(Player player, bool includeContainers, float containerRange)
        {
            _inventories.Add(player.GetInventory());
            if (includeContainers)
                AddContainers(player, containerRange);
        }

        private void AddContainers(Player player, float range)
        {
            Vector3 origin = player.transform.position;
            long playerId = player.GetPlayerID();

            s_pieces.Clear();
            Piece.GetAllPiecesInRadius(origin, range, s_pieces);
            foreach (Piece piece in s_pieces.OrderBy(p => Vector3.Distance(origin, p.transform.position)))
            {
                // Player-built only: skips dungeon/treasure chests.
                if (piece.GetCreator() == 0L)
                    continue;
                Container container = piece.GetComponentInChildren<Container>();
                if (container && IsUsable(container, playerId))
                {
                    _inventories.Add(container.GetInventory());
                    _containers[container.GetInventory()] = container;
                }
            }
        }

        private static bool IsUsable(Container container, long playerId)
        {
            ZNetView nview = container.m_nview;
            if (!nview || !nview.IsValid() || container.GetInventory() == null || container.m_autoDestroyEmpty)
                return false;
            // s_inUse is synced by the owner while someone has the chest open.
            if (container.IsInUse() || nview.GetZDO().GetInt(ZDOVars.s_inUse) != 0)
                return false;
            if (container.m_wagon && container.m_wagon.InUse())
                return false;
            return container.CheckAccess(playerId) && PrivateArea.CheckAccess(container.transform.position, 0f, flash: false);
        }

        /// <summary>Removes up to <paramref name="wanted"/> of an item across all sources and returns how many were taken.</summary>
        public int Take(ItemDrop item, int wanted, out bool cheated)
        {
            cheated = false;
            string name = item.m_itemData.m_shared.m_name;
            int taken = 0;

            foreach (Inventory inventory in _inventories)
            {
                if (taken >= wanted)
                    break;
                int count = Math.Min(wanted - taken, inventory.CountItems(name));
                if (count <= 0)
                    continue;

                if (_containers.TryGetValue(inventory, out Container container))
                {
                    // Containers only save when owned locally; take ownership the way opening one does,
                    // otherwise the removal is reverted on the next sync and the items are duplicated.
                    if (!container.IsOwner())
                        container.m_nview.ClaimOwnership();
                    _usedContainers.Add(container);
                }

                cheated |= inventory.GetAllItems().Any(i => i.m_shared.m_name == name && i.m_cheated);
                inventory.RemoveItem(name, count);
                taken += count;
            }

            if (taken > 0)
                Taken[name] = Taken.TryGetValue(name, out int previous) ? previous + taken : taken;
            return taken;
        }
    }
}
