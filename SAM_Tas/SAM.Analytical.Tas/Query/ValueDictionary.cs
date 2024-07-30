using System;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, Tuple<double, int>> ValueDictionary(this BuildingData buildingData, tsdZoneArray tsdZoneArray)
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

            object[,] values = buildingData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray }) as dynamic;

            Dictionary<string, Tuple<double, int>> dictionary = new Dictionary<string, Tuple<double, int>>();
            for (int i = 0; i < zoneDatas.Count; i++)
            {
                dictionary[zoneDatas[i].zoneGUID] = new Tuple<double, int> ((float)values[1, i], (int)values[2, i]);
            }

            return dictionary;
        }
    }
}