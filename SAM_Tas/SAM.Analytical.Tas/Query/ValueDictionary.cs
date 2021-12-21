using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, double> ValueDictionary(this BuildingData buildingData, tsdZoneArray tsdZoneArray)
        {
            if(buildingData == null)
            {
                return null;
            }

            List<ZoneData> zoneDatas = buildingData.ZoneDatas();
            if(zoneDatas == null || zoneDatas.Count == 0)
            {
                return null;
            }

            object[,] values = buildingData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray });

            Dictionary<string, double> dictionary = new Dictionary<string, double>();
            for (int i = 0; i < zoneDatas.Count; i++)
            {
                dictionary[zoneDatas[i].zoneGUID] = (float)values[1, i];
            }

            return dictionary;
        }
    }
}