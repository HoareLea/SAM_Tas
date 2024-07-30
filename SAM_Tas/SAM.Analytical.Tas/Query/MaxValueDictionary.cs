using System;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, Tuple<double, int, CoolingDesignData>> MaxValueDictionary(this IEnumerable<CoolingDesignData> coolingDesignDatas, tsdZoneArray tsdZoneArray) 
        {
            if(coolingDesignDatas == null)
            {
                return null;
            }

            Dictionary<string, Tuple<double, int, CoolingDesignData>> result = new Dictionary<string, Tuple<double, int, CoolingDesignData>>();

            if(coolingDesignDatas.Count() == 0)
            {
                return result;
            }

            CoolingDesignData coolingDesignData = coolingDesignDatas.ElementAt(0);
            if(coolingDesignData == null)
            {
                return result;
            }


            List<ZoneData> zoneDatas = coolingDesignData.ZoneDatas();
            if(zoneDatas == null || zoneDatas.Count == 0)
            {
                return result;
            }

            object[,] values = coolingDesignData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray }) as dynamic;

            for(int i=0; i < zoneDatas.Count; i++)
            {
                string id = zoneDatas[i].zoneGUID;
                double value = (float)values[1, i];
                int index = (int)values[2, i];

                result[id] = new Tuple<double, int, CoolingDesignData>(value, index, coolingDesignData);
            }

            for (int i = 1; i < coolingDesignDatas.Count(); i++)
            {
                values = coolingDesignDatas.ElementAt(i).GetPeakZoneGains(new short[1] { (short)tsdZoneArray }) as dynamic;
                for (int j = 0; j < zoneDatas.Count; j++)
                {
                    string id = zoneDatas[j].zoneGUID;
                    double value = (float)values[1, j];
                    int index = (int)values[2, j];

                    if (!result.TryGetValue(id, out Tuple<double, int, CoolingDesignData> tuple))
                    {
                        result[id] = new Tuple<double, int, CoolingDesignData>(value, index, coolingDesignData);
                    }
                    else if(tuple.Item1 < value)
                    {
                        tuple = new Tuple<double, int, CoolingDesignData>(value, index, coolingDesignDatas.ElementAt(i));
                        result[id] = tuple;
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, Tuple<double, int, HeatingDesignData>> MaxValueDictionary(this IEnumerable<HeatingDesignData> heatingDesignDatas, tsdZoneArray tsdZoneArray)
        {
            if (heatingDesignDatas == null)
            {
                return null;
            }

            Dictionary<string, Tuple<double, int, HeatingDesignData>> result = new Dictionary<string, Tuple<double, int, HeatingDesignData>>();

            if (heatingDesignDatas.Count() == 0)
            {
                return result;
            }

            HeatingDesignData heatingDesignData = heatingDesignDatas.ElementAt(0);
            if (heatingDesignData == null)
            {
                return result;
            }


            List<ZoneData> zoneDatas = heatingDesignData.ZoneDatas();
            if (zoneDatas == null || zoneDatas.Count == 0)
            {
                return result;
            }

            object[,] values = heatingDesignData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray }) as dynamic;

            for (int i = 0; i < zoneDatas.Count; i++)
            {
                string id = zoneDatas[i].zoneGUID;
                double value = (float)values[1, i];
                int index = (int)values[2, i];

                result[id] = new Tuple<double, int, HeatingDesignData>(value, index, heatingDesignData);
            }

            for (int i = 1; i < heatingDesignDatas.Count(); i++)
            {
                values = heatingDesignDatas.ElementAt(i).GetPeakZoneGains(new short[1] { (short)tsdZoneArray }) as dynamic;
                for (int j = 0; j < zoneDatas.Count; j++)
                {
                    string id = zoneDatas[j].zoneGUID;
                    double value = (float)values[1, j];
                    int index = (int)values[2, j];

                    if (!result.TryGetValue(id, out Tuple<double, int, HeatingDesignData> tuple))
                    {
                        result[id] = new Tuple<double, int, HeatingDesignData>(value, index, heatingDesignData);
                    }
                    else if (tuple.Item1 < value)
                    {
                        tuple = new Tuple<double, int, HeatingDesignData>(value, index, heatingDesignDatas.ElementAt(i));
                        result[id] = tuple;
                    }
                }
            }

            return result;
        }
    }
}