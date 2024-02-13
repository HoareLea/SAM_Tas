using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<ZoneData> ZoneDatas(this BuildingData buildingData)
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

        public static List<ZoneData> ZoneDatas(this ZoneDataGroup zoneDataGroup)
        {
            if (zoneDataGroup == null)
                return null;

            List<ZoneData> result = new List<ZoneData>();

            int index = 1;
            ZoneData zoneData = zoneDataGroup.GetZoneData(index);
            while (zoneData != null)
            {
                result.Add(zoneData);
                index++;
                zoneData = zoneDataGroup.GetZoneData(index);
            }
            return result;
        }

        public static List<ZoneData> ZoneDatas(this CoolingDesignData coolingDesignData)
        {
            if (coolingDesignData == null)
                return null;

            List<ZoneData> result = new List<ZoneData>();

            int index = 1;
            ZoneData zoneData = coolingDesignData.GetZoneData(index);
            while (zoneData != null)
            {
                result.Add(zoneData);
                index++;
                zoneData = coolingDesignData.GetZoneData(index);
            }
            return result;
        }

        public static List<ZoneData> ZoneDatas(this HeatingDesignData heatingDesignData)
        {
            if (heatingDesignData == null)
                return null;

            List<ZoneData> result = new List<ZoneData>();

            int index = 1;
            ZoneData zoneData = heatingDesignData.GetZoneData(index);
            while (zoneData != null)
            {
                result.Add(zoneData);
                index++;
                zoneData = heatingDesignData.GetZoneData(index);
            }
            return result;
        }
    }
}