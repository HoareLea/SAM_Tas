using System;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, Tuple<double, CoolingDesignData>> MaxValueDictionary(this IEnumerable<CoolingDesignData> coolingDesignDatas, tsdZoneArray tsdZoneArray) 
        {
            if(coolingDesignDatas == null)
            {
                return null;
            }

            Dictionary<string, Tuple<double, CoolingDesignData>> result = new Dictionary<string, Tuple<double, CoolingDesignData>>();

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

            object[,] values = coolingDesignData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray });

            for(int i=0; i < zoneDatas.Count; i++)
            {
                string id = zoneDatas[i].zoneGUID;
                double value = (float)values[1, i];

                result[id] = new Tuple<double, CoolingDesignData>(value, coolingDesignData);
            }

            for (int i = 1; i < coolingDesignDatas.Count(); i++)
            {
                values = coolingDesignDatas.ElementAt(i).GetPeakZoneGains(new short[1] { (short)tsdZoneArray });
                for (int j = 0; j < zoneDatas.Count; j++)
                {
                    string id = zoneDatas[j].zoneGUID;
                    double value = (float)values[1, j];

                    if(!result.TryGetValue(id, out Tuple<double, CoolingDesignData> tuple))
                    {
                        result[id] = new Tuple<double, CoolingDesignData>(value, coolingDesignData);
                    }
                    else if(tuple.Item1 < value)
                    {
                        tuple = new Tuple<double, CoolingDesignData>(value, coolingDesignDatas.ElementAt(i));
                        result[id] = tuple;
                    }
                }
            }

            return result;
        }

        public static Dictionary<string, Tuple<double, HeatingDesignData>> MaxValueDictionary(this IEnumerable<HeatingDesignData> heatingDesignDatas, tsdZoneArray tsdZoneArray)
        {
            if (heatingDesignDatas == null)
            {
                return null;
            }

            Dictionary<string, Tuple<double, HeatingDesignData>> result = new Dictionary<string, Tuple<double, HeatingDesignData>>();

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

            object[,] values = heatingDesignData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray });

            for (int i = 0; i < zoneDatas.Count; i++)
            {
                string id = zoneDatas[i].zoneGUID;
                double value = (float)values[1, i];

                result[id] = new Tuple<double, HeatingDesignData>(value, heatingDesignData);
            }

            for (int i = 1; i < heatingDesignDatas.Count(); i++)
            {
                values = heatingDesignDatas.ElementAt(i).GetPeakZoneGains(new short[1] { (short)tsdZoneArray });
                for (int j = 0; j < zoneDatas.Count; j++)
                {
                    string id = zoneDatas[j].zoneGUID;
                    double value = (float)values[1, j];

                    if (!result.TryGetValue(id, out Tuple<double, HeatingDesignData> tuple))
                    {
                        result[id] = new Tuple<double, HeatingDesignData>(value, heatingDesignData);
                    }
                    else if (tuple.Item1 < value)
                    {
                        tuple = new Tuple<double, HeatingDesignData>(value, heatingDesignDatas.ElementAt(i));
                        result[id] = tuple;
                    }
                }
            }

            return result;
        }
    }
}