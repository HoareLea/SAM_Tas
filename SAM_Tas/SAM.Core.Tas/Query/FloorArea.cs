using System.Collections.Generic;
using TSD;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static double FloorArea(this BuildingData buildingData)
        {
            if(buildingData is null)
            {
                return double.NaN;
            }

            double result = 0;

            int index = 1;
            ZoneData zoneData = buildingData.GetZoneData(index);
            while (zoneData != null)
            {
                result += zoneData.floorArea;
                index++;
                zoneData = buildingData.GetZoneData(index);
            }

            return result;
        }
    }
}