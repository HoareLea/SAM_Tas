using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TAS3D.zoneSet> ZoneSets(this TAS3D.Building building)
        {
            List<TAS3D.zoneSet> result = new List<TAS3D.zoneSet>();

            int index = 1;
            TAS3D.zoneSet zoneSet = building.GetZoneSet(index);
            while (zoneSet != null)
            {
                result.Add(zoneSet);
                index++;

                zoneSet = building.GetZoneSet(index);
            }

            return result;
        }
    }
}