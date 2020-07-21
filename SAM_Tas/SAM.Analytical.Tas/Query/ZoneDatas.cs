using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TSD.ZoneData> ZoneDatas(this BuildingData buildingData)
        {
            if (buildingData == null)
                return null;

            List<ZoneData> result = new List<ZoneData>();

            int index = 1;
            ZoneData zoneData = buildingData.GetZoneData(index);
            while (zoneData != null)
            {
                result.Add(zoneData);
                index++;
                zoneData = buildingData.GetZoneData(index);
            }
            return result;
        }

        public static List<ZoneData> ZoneDatas(this CoolingDesignData CoolingDesignData)
        {
            if (CoolingDesignData == null)
                return null;

            List<ZoneData> result = new List<ZoneData>();

            int index = 1;
            ZoneData zoneData = CoolingDesignData.GetZoneData(index);
            while (zoneData != null)
            {
                result.Add(zoneData);
                index++;
                zoneData = CoolingDesignData.GetZoneData(index);
            }
            return result;
        }
    }
}