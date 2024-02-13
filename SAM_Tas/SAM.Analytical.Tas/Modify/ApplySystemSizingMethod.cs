using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool ApplySystemSizingMethod(this zone zone, AdjacencyCluster adjacencyCluster)
        {
            if (zone == null || adjacencyCluster == null)
            {
                return false;
            }

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null || spaces.Count == 0)
            {
                return false;
            }

            Space space = spaces.Match(zone.name, true, false);
            if (space == null)
            {
                space = spaces.Match(zone.name, false, true);
            }

            if (space == null)
            {
                return false;
            }

            List<MechanicalSystem> mechanicalSystems = adjacencyCluster.GetRelatedObjects<MechanicalSystem>(space);
            if (mechanicalSystems == null || mechanicalSystems.Count == 0)
            {
                return false;
            }

            if (mechanicalSystems.Find(x => x is CoolingSystem && x.Name == "UC") == null && mechanicalSystems.Find(x => x is HeatingSystem && x.Name == "UH") == null)
            {
                return false;
            }

            if (mechanicalSystems?.Find(x => x.Name == "UC") != null)
            {
                zone.sizeCooling = (int)TBD.SizingType.tbdNoSizing;
                zone.maxCoolingLoad = 0;
            }

            if (mechanicalSystems?.Find(x => x.Name == "UH") != null)
            {
                zone.sizeHeating = (int)TBD.SizingType.tbdNoSizing;
                zone.maxHeatingLoad = 0;
            }

            return true;
        }
    }
}