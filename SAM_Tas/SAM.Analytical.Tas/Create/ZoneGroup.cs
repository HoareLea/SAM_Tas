using System.Collections.Generic;
using System.Linq;

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

            List<Space> spaces = adjacencyCluster.GetRelatedObjects<Space>(zone_Temp);
            TBD.ZoneGroup result = ZoneGroup(building, zone_Temp.Name, spaces);

            if (zone_Temp.TryGetValue(Analytical.ZoneParameter.ZoneCategory, out string zoneCategory))
            {
                result.description = zoneCategory;
            }

            return result;
        }

        public static TBD.ZoneGroup ZoneGroup(this TBD.Building building, string name, IEnumerable<Space> spaces, TBD.ZoneGroupType zoneGroupType = TBD.ZoneGroupType.tbdDefaultZG)
        {
            if (building == null || spaces == null|| string.IsNullOrEmpty(name))
            {
                return null;
            }

            TBD.ZoneGroup result = building.ZoneGroups()?.Find(x => x.name == name && x.type == (int)zoneGroupType);
            if(result == null)
            {
                result = building.AddZoneGroup();
                result.name = name;
                result.type = (int)zoneGroupType;
            }

            if (spaces != null || spaces.Count() != 0)
            {
                foreach (Space space in spaces)
                {
                    if (string.IsNullOrWhiteSpace(space?.Name))
                    {
                        continue;
                    }

                    TBD.zone zone_TBD = building.Zone(space.Name);
                    if (zone_TBD == null)
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