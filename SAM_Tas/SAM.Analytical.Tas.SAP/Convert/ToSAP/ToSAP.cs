using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.SAP
{
    public static partial class Convert
    {
        public static SAPData ToSAP(this AnalyticalModel analyticalModel, string zoneCategory = null, TextMap textMap = null)
        {
            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if(adjacencyCluster == null)
            {
                return null;
            }

            SAPData result = new SAPData();

            List<Zone> zones = adjacencyCluster.GetZones();
            if(zones != null)
            {
                if(zoneCategory != null)
                {
                    for(int i = zones.Count - 1; i >= 0; i--)
                    {
                        if(zones[i] == null || !zones[i].TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory_Temp) || zoneCategory != zoneCategory_Temp)
                        {
                            zones.RemoveAt(i);
                        }
                    }
                }

                foreach(Zone zone in zones)
                {
                    List<Space> spaces = adjacencyCluster.GetSpaces(zone);
                }
            }

            return result;
        }
    }
}
