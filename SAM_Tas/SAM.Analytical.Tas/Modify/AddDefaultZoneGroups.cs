using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<TBD.ZoneGroup> AddDefaultZoneGroups(this TBD.Building building, AdjacencyCluster adjacencyCluster, string undefinedZoneGroupName = "Undefined", string allZoneGroupName = "All")
        {
            if (building == null || adjacencyCluster == null)
            {
                return null;
            }

            List<TBD.ZoneGroup> result = new List<TBD.ZoneGroup>();

            List<Space> spaces_Unzoned = adjacencyCluster.GetSpaces();
            if (!string.IsNullOrEmpty(allZoneGroupName) && spaces_Unzoned != null && spaces_Unzoned.Count != 0)
            {
                TBD.ZoneGroup zoneGroup = building.ZoneGroups()?.Find(x => x.name == allZoneGroupName && x.type == (int)TBD.ZoneGroupType.tbdZoneSetZG);
                if(zoneGroup == null)
                {
                    zoneGroup = building.ZoneGroup(allZoneGroupName, spaces_Unzoned, TBD.ZoneGroupType.tbdZoneSetZG);
                }

                if(zoneGroup != null)
                {
                    result.Add(zoneGroup);
                }
            }

            List<Zone> zones = adjacencyCluster.GetZones();
            if (zones != null && zones.Count != 0)
            {
                foreach (Zone zone in zones)
                {
                    building.ZoneGroup(adjacencyCluster, zone);

                    List<Space> spaces_Temp = adjacencyCluster.GetSpaces(zone);
                    if (spaces_Temp != null && spaces_Temp.Count != 0)
                    {
                        foreach (Space space in spaces_Temp)
                        {
                            spaces_Unzoned.RemoveAll(x => x.Guid == space.Guid);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(undefinedZoneGroupName) && spaces_Unzoned != null && spaces_Unzoned.Count != 0)
            {
                TBD.ZoneGroup zoneGroup = building.ZoneGroup(undefinedZoneGroupName, spaces_Unzoned);
                if (zoneGroup != null)
                {
                    result.Add(zoneGroup);
                }
            }

            return result;
        }
    }
}