using System.Collections.Generic;
using System.Xml;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        public static Building ToTM59(this AnalyticalModel analyticalModel, TM59Manager tM59Manager)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            List<Zone> zones = new List<Zone>();

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                List<Space> spaces = adjacencyCluster?.GetSpaces();
                if (spaces != null)
                {
                    foreach (Space space in spaces)
                    {
                        Zone zone = space.ToTM59(tM59Manager);
                        if (zone != null)
                        {
                            zones.Add(zone);
                        }
                    }
                }
            }

            return new Building(BuildingCategory.Category_II, false, false, zones);
        }
    }
}
