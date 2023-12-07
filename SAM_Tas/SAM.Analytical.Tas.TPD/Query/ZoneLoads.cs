using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ZoneLoad> ZoneLoads(this SystemZone systemZone)
        { 
            if(systemZone == null)
            {
                return null;
            }

            return ZoneLoads((SystemComponent)systemZone);
        }

        public static List<ZoneLoad> ZoneLoads(this SystemComponent systemComponent)
        {
            if (systemComponent == null)
            {
                return null;
            }

            int index = 1;

            dynamic @dynamic = systemComponent;

            List<ZoneLoad> result = new List<ZoneLoad>();

            int count = @dynamic.GetZoneLoadCount();
            for (int i = 1; i <= count; i++)
            {
                ZoneLoad zoneLoad = @dynamic.GetZoneLoad(index);
                result.Add(zoneLoad);
            }

            return result;
        }
    }
}