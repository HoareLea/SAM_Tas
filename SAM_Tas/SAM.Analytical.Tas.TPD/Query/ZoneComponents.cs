using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<T> ZoneComponents<T>(this SystemZone systemZone) where T: ZoneComponent
        { 
            if(systemZone == null)
            {
                return null;
            }

            int index = 1;

            List<T> result = new List<T>();

            ZoneComponent zoneComponent = systemZone.GetZoneComponent(index);
            while(zoneComponent != null)
            {
                if(zoneComponent is T)
                {
                    result.Add((T)zoneComponent);
                }

                index++;
                zoneComponent = systemZone.GetZoneComponent(index);
            }

            return result;
        }
    }
}