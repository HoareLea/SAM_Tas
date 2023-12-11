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

            int count = systemZone.GetZoneComponentCount();
            for (int i = 1; i <= count; i++)
            {
                ZoneComponent zoneComponent = systemZone.GetZoneComponent(i);
                if (zoneComponent is T)
                {
                    result.Add((T)zoneComponent);
                }
            }

            return result;
        }
    }
}