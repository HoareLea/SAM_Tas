using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, ZoneData> ZoneDataDictionary(this IEnumerable<ZoneData> zoneDatas)
        {
            if (zoneDatas == null)
                return null;

            Dictionary<string, ZoneData> result = new Dictionary<string, ZoneData>();

            foreach(ZoneData zoneData in zoneDatas)
            {
                string reference = zoneData.zoneGUID;
                if (reference == null)
                    continue;

                result[reference] = zoneData;
            }
            return result;
        }

        public static Dictionary<string, ZoneData> ZoneDataDictionary(this BuildingData buildingData)
        {
            if (buildingData == null)
                return null;

            Dictionary<string, ZoneData> result = new Dictionary<string, ZoneData>();

            int index = 1;
            ZoneData zoneData = buildingData.GetZoneData(index);
            while (zoneData != null)
            {
                string reference = zoneData.zoneGUID;
                if (reference == null)
                    continue;

                result[reference] = zoneData;
                index++;
                zoneData = buildingData.GetZoneData(index);
            }
            return result;
        }

        public static Dictionary<string, ZoneData> ZoneDataDictionary(this CoolingDesignData coolingDesignData)
        {
            if (coolingDesignData == null)
                return null;

            Dictionary<string, ZoneData> result = new Dictionary<string, ZoneData>();

            int index = 1;
            ZoneData zoneData = coolingDesignData.GetZoneData(index);
            while (zoneData != null)
            {
                string reference = zoneData.zoneGUID;
                if (reference == null)
                    continue;

                result[reference] = zoneData;
                index++;
                zoneData = coolingDesignData.GetZoneData(index);
            }
            return result;
        }

        public static Dictionary<string, ZoneData> ZoneDataDictionary(this HeatingDesignData heatingDesignData)
        {
            if (heatingDesignData == null)
                return null;

            Dictionary<string, ZoneData> result = new Dictionary<string, ZoneData>();

            int index = 1;
            ZoneData zoneData = heatingDesignData.GetZoneData(index);
            while (zoneData != null)
            {
                string reference = zoneData.zoneGUID;
                if (reference == null)
                    continue;
                
                result[reference] = zoneData;
                index++;
                zoneData = heatingDesignData.GetZoneData(index);
            }
            return result;
        }
    }
}