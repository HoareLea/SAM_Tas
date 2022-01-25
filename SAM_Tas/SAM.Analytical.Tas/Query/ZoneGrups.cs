using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.ZoneGroup> ZoneGroups(this TBD.Building building)
        {
            if (building == null)
                return null;

            List<TBD.ZoneGroup> result = new List<TBD.ZoneGroup>();

            TBD.ZoneGroup aZone = building.GetZoneGroup(result.Count);
            while (aZone != null)
            {
                result.Add(aZone);
                aZone = building.GetZoneGroup(result.Count);
            }

            return result;
        }
    }
}