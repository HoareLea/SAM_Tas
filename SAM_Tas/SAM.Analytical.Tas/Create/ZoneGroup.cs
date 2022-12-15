using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static TBD.ZoneGroup ZoneGroup(this TBD.Building building, AdjacencyCluster adjacencyCluster, Zone zone)
        {
            if (building == null || adjacencyCluster == null)
            {
                return null;
            }

            Zone zone_Temp = adjacencyCluster.GetObject<Zone>(zone.Guid);
            if (zone_Temp == null)
            {
                return null;
            }

            TBD.ZoneGroup result = building.AddZoneGroup();
            result.name = zone_Temp.Name;
            result.type = (int)TBD.ZoneGroupType.tbdDefaultZG;
            if(zone_Temp.TryGetValue(ZoneParameter.ZoneCategory, out string zoneCategory))
            {
                result.description = zoneCategory;
            }

            List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(zone_Temp);
            if (spaces != null || spaces.Count != 0)
            {
                foreach(Space space in spaces)
                {
                    if(string.IsNullOrWhiteSpace(space?.Name))
                    {
                        continue;
                    }

                    TBD.zone zone_TBD = building.Zone(space.Name);
                    if(zone_TBD == null)
                    {
                        continue;
                    }

                    result.InsertZone(zone_TBD);
                }
            }

            return result;
        }
    }
}