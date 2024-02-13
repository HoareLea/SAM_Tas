using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<ZoneDataGroup> ZoneDataGroups(this SimulationData simulationData)
        {
            if (simulationData == null)
                return null;

            List<ZoneDataGroup> result = new List<ZoneDataGroup>();

            int index = 1;
            ZoneDataGroup zoneDataGroup = simulationData.GetZoneDataGroup(index);
            while (zoneDataGroup != null)
            {
                result.Add(zoneDataGroup);
                index++;
                zoneDataGroup = simulationData.GetZoneDataGroup(index);
            }
            return result;
        }
    }
}