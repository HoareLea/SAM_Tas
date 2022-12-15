using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TBD.zone Zone(this TBD.Building building, string name)
        {
            if (building == null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            List<TBD.zone> zones = building.Zones();
            if(zones == null || zones.Count == 0)
            {
                return null;
            }

            return zones.Find(x => x.name == name);
        }
    }
}