using System;
using System.Collections.Generic;
using System.Linq;

namespace QuickFill
{
    /// <summary>One station of a group of identical smelters, as seen by <see cref="SmelterPlan"/>.</summary>
    internal class StationState
    {
        public int Queued;          // ore already queued
        public int OreSpace;        // free ore slots
        public float Fuel;          // fuel already inside
        public int FuelSpace;       // free fuel slots
        public int FuelPerProduct;  // 0 for stations without fuel (windmills, kilns, spinning wheels...)

        // Plan output.
        public readonly List<int> Units = new List<int>(); // input index (into the plan's inputs) of each ore unit added
        public int PlannedFuel;                            // fuel to add so the planned ore can be smelted

        public int Load => Queued + Units.Count;

        /// <summary>Fuel this station still needs to smelt its queue plus <paramref name="added"/> more ore, or -1 if it can't hold that much.</summary>
        public int FuelNeeded(int added)
        {
            int need = Math.Max(0, (int)Math.Ceiling((Queued + added) * FuelPerProduct - Fuel - 0.0001f));
            return need > FuelSpace ? -1 : need;
        }
    }

    /// <summary>
    /// Splits ore between identical smelters (all furnaces, all windmills...) so they work in parallel.
    /// When the fuel on hand can't back an even split, ore goes where it can be smelted with the least fuel instead,
    /// even if that means a single station.
    /// </summary>
    internal static class SmelterPlan
    {
        /// <param name="stations">Stations, nearest first; nearer ones win ties.</param>
        /// <param name="available">Units available of each input, highest grade first.</param>
        /// <param name="fuelBudget">Fuel available to the whole group.</param>
        public static void Plan(IList<StationState> stations, IList<int> available, int fuelBudget)
        {
            var units = new List<int>();
            int space = stations.Sum(s => Math.Max(0, s.OreSpace));
            for (int input = 0; input < available.Count && units.Count < space; input++)
                for (int i = 0; i < available[input] && units.Count < space; i++)
                    units.Add(input);

            PlanEven(stations, units);
            if (stations.Sum(FuelNeededOrFull) <= fuelBudget)
            {
                foreach (StationState s in stations)
                    s.PlannedFuel = FuelNeededOrFull(s);
                return;
            }

            foreach (StationState s in stations)
                s.Units.Clear();
            PlanFuelEfficient(stations, units, fuelBudget);
        }

        private static int FuelNeededOrFull(StationState s)
        {
            if (s.FuelPerProduct <= 0)
                return 0;
            int need = s.FuelNeeded(s.Units.Count);
            return need < 0 ? s.FuelSpace : need;
        }

        /// <summary>Each unit goes to the least loaded station with room.</summary>
        private static void PlanEven(IList<StationState> stations, List<int> units)
        {
            foreach (int unit in units)
            {
                StationState target = Pick(stations, s => HasRoom(s), s => 0);
                if (target == null)
                    break;
                target.Units.Add(unit);
            }
        }

        /// <summary>
        /// Each unit goes where it costs the least new fuel, then to the least loaded station. Once fuel runs out,
        /// the rest go to stations that already have fuel, so they can finish when more arrives, instead of
        /// sitting in cold ones.
        /// </summary>
        private static void PlanFuelEfficient(IList<StationState> stations, List<int> units, int fuelBudget)
        {
            foreach (int unit in units)
            {
                StationState target = Pick(stations, s => HasRoom(s) && Cost(s) >= 0 && Cost(s) <= fuelBudget, Cost);
                if (target != null)
                {
                    int cost = Cost(target);
                    fuelBudget -= cost;
                    target.PlannedFuel += cost;
                }
                else
                {
                    target = Pick(stations, s => HasRoom(s) && (s.PlannedFuel > 0 || s.Fuel > 0f), s => 0)
                        ?? Pick(stations, s => HasRoom(s), s => 0);
                    if (target == null)
                        break;
                }
                target.Units.Add(unit);
            }
        }

        private static bool HasRoom(StationState s) => s.Units.Count < s.OreSpace;

        /// <summary>Extra fuel needed to smelt one more unit at this station, or -1 if it can't hold that much fuel.</summary>
        private static int Cost(StationState s)
        {
            int before = s.FuelNeeded(s.Units.Count);
            int after = s.FuelNeeded(s.Units.Count + 1);
            return before < 0 || after < 0 ? -1 : after - before;
        }

        private static StationState Pick(IList<StationState> stations, Func<StationState, bool> eligible, Func<StationState, int> cost)
        {
            StationState best = null;
            foreach (StationState s in stations)
            {
                if (!eligible(s))
                    continue;
                if (best == null || cost(s) < cost(best) || (cost(s) == cost(best) && s.Load < best.Load))
                    best = s;
            }
            return best;
        }
    }
}
