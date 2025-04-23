using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static HashSet<string> UpdateZoneGroups(this Building building, AdjacencyCluster adjacencyCluster)
        {
            List<zone> zones_TBD = building?.Zones();
            if (zones_TBD == null || zones_TBD.Count == 0)
            {
                return null;
            }

            List<Zone> zones_SAM = adjacencyCluster.GetZones();
            if (zones_SAM == null || zones_SAM.Count == 0)
            {
                return null;
            }

            HashSet<string> result = new HashSet<string>();

            foreach (Zone zone_SAM in zones_SAM)
            {
                if (string.IsNullOrWhiteSpace(zone_SAM?.Name))
                {
                    continue;
                }

                string name = zone_SAM.Name;

                if (!zone_SAM.TryGetValue(Analytical.ZoneParameter.ZoneCategory, out string zoneCategory) || string.IsNullOrWhiteSpace(zoneCategory))
                {
                    zoneCategory = null;
                }

                List<ZoneGroup> zoneGroups = building.ZoneGroups();
                if (zoneGroups != null)
                {
                    int index = zoneGroups.FindIndex(x => x.name == name && x.type == (int)ZoneGroupType.tbdDefaultZG);
                    if (index != -1)
                    {
                        building.RemoveZoneGroup(index);
                    }
                }

                ZoneGroup zoneGroup = Create.ZoneGroup(building, adjacencyCluster, zone_SAM);
                if(!string.IsNullOrWhiteSpace(zoneGroup?.name))
                {
                    result.Add(zoneGroup.name);
                }
            }

            return result;
        }

        public static HashSet<string> UpdateZoneGroups(this TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
            {
                return null;
            }

            return UpdateZoneGroups(tBDDocument.Building, analyticalModel.AdjacencyCluster);
        }
    }
}