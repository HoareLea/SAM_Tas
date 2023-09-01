using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static HashSet<string> UpdateZoneGroups(this TBD.TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
            {
                return null;
            }

            List<TBD.zone> zones_TBD = tBDDocument.Building.Zones();
            if (zones_TBD == null || zones_TBD.Count == 0)
            {
                return null;
            }


            List<Zone> zones_SAM = analyticalModel.GetZones();
            if(zones_SAM == null || zones_SAM.Count == 0)
            {
                return null;
            }

            HashSet<string> result = new HashSet<string>();

            foreach (Zone zone_SAM in zones_SAM)
            {
                if(string.IsNullOrWhiteSpace(zone_SAM?.Name))
                {
                    continue;
                }

                string name = zone_SAM.Name;

                List<TBD.ZoneGroup> zoneGroups = tBDDocument.Building.ZoneGroups();
                if(zoneGroups != null)
                {
                    int index = zoneGroups.FindIndex(x => x.name == name && x.type == (int)TBD.ZoneGroupType.tbdDefaultZG);
                    if (index != -1)
                    {
                        tBDDocument.Building.RemoveZoneGroup(index);
                    }
                }

                TBD.ZoneGroup zoneGroup = tBDDocument.Building.AddZoneGroup();
                zoneGroup.name = name;
                zoneGroup.type = (int)TBD.ZoneGroupType.tbdDefaultZG;
                
                result.Add(zoneGroup.name);

                List<Space> spaces_SAM = analyticalModel.GetSpaces(zone_SAM);
                if(spaces_SAM == null || spaces_SAM.Count == 0)
                {
                    continue;
                }

                foreach(Space space_SAM in spaces_SAM)
                {
                    TBD.zone zone = space_SAM.Match(zones_TBD);
                    if (zone == null)
                    {
                        continue;
                    }

                    zoneGroup.InsertZone(zone);
                }
            }

            return result;
        }
    }
}