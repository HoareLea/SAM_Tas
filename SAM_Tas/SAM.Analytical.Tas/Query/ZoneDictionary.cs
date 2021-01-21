using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, TAS3D.Zone> ZoneDictionary(this TAS3D.Building building)
        {
            Dictionary<string, TAS3D.Zone> result = new Dictionary<string, TAS3D.Zone>();

            int index = 1;
            TAS3D.Zone zone = building.GetZone(index);
            while (zone != null)
            {
                string name = zone.name;
                if(name != null)
                    result[name] = zone;

                index++;

                zone = building.GetZone(index);
            }

            return result;
        }

        public static Dictionary<string, TBD.zone> ZoneDictionary(this TBD.Building building)
        {
            Dictionary<string, TBD.zone> result = new Dictionary<string, TBD.zone>();

            int index = 0;
            TBD.zone zone = building.GetZone(index);
            while (zone != null)
            {
                string name = zone.name;
                if (name != null)
                    result[name] = zone;

                index++;

                zone = building.GetZone(index);
            }

            return result;
        }
    }
}