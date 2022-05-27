using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> DesignDataDictionary(this SAMTSDDocument sAMTSDDocument)
        {
            return DesignDataDictionary(sAMTSDDocument?.TSDDocument?.SimulationData);

        }

        public static Dictionary<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> DesignDataDictionary(this SimulationData simulationData)
        {
            if (simulationData == null)
            {
                return null;
            }

            Dictionary<string, Tuple<double, int, CoolingDesignData>> dictionary_DesignDay_Cooling = MaxValueDictionary(simulationData.CoolingDesignDatas(), tsdZoneArray.coolingLoad);
            Dictionary<string, Tuple<double, int, HeatingDesignData>> dictionary_DesignDay_Heating = MaxValueDictionary(simulationData.HeatingDesignDatas(), tsdZoneArray.heatingLoad);

            //BuildingData buildingData = simulationData.GetBuildingData();
            //if (buildingData != null)
            //{
            //    Dictionary<string, Tuple<double, int>> dictionary = null;

            //    dictionary = ValueDictionary(buildingData, tsdZoneArray.coolingLoad);
            //    if (dictionary != null && dictionary_DesignDay_Cooling != null)
            //    {
            //        foreach (KeyValuePair<string, Tuple<double, int>> keyValuePair in dictionary)
            //        {
            //            if (!dictionary_DesignDay_Cooling.TryGetValue(keyValuePair.Key, out Tuple<double, int, CoolingDesignData> tuple) || tuple == null)
            //            {
            //                continue;
            //            }

            //            if (tuple.Item1 < keyValuePair.Value.Item1)
            //            {
            //                tuple = new Tuple<double, int, CoolingDesignData>(keyValuePair.Value.Item1, keyValuePair.Value.Item2, null);
            //                dictionary_DesignDay_Cooling[keyValuePair.Key] = tuple;
            //            }
            //        }
            //    }

            //    dictionary = ValueDictionary(buildingData, tsdZoneArray.heatingLoad);
            //    if (dictionary != null && dictionary_DesignDay_Heating != null)
            //    {
            //        foreach (KeyValuePair<string, Tuple<double, int>> keyValuePair in dictionary)
            //        {
            //            if (!dictionary_DesignDay_Heating.TryGetValue(keyValuePair.Key, out Tuple<double, int, HeatingDesignData> tuple) || tuple == null)
            //            {
            //                continue;
            //            }

            //            if (tuple.Item1 < keyValuePair.Value.Item1)
            //            {
            //                tuple = new Tuple<double, int, HeatingDesignData>(keyValuePair.Value.Item1, keyValuePair.Value.Item2, null);
            //                dictionary_DesignDay_Heating[keyValuePair.Key] = tuple;
            //            }
            //        }
            //    }
            //}

            Dictionary<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> result = new Dictionary<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>>();
            if (dictionary_DesignDay_Cooling != null)
            {
                foreach (KeyValuePair<string, Tuple<double, int, CoolingDesignData>> keyValuePair_Cooling in dictionary_DesignDay_Cooling)
                {
                    Tuple<double, int, HeatingDesignData> tuple_Heating = null;
                    if (dictionary_DesignDay_Heating == null || !dictionary_DesignDay_Heating.TryGetValue(keyValuePair_Cooling.Key, out tuple_Heating))
                    {
                        tuple_Heating = null;
                    }

                    double coolingLoad = keyValuePair_Cooling.Value.Item1;
                    int coolingIndex = keyValuePair_Cooling.Value.Item2;
                    CoolingDesignData coolingDesignData = keyValuePair_Cooling.Value.Item3;
                    double heatingLoad = double.NaN;
                    int heatingIndex = -1;
                    HeatingDesignData heatingDesignData = null;
                    if (tuple_Heating != null)
                    {
                        heatingLoad = tuple_Heating.Item1;
                        heatingDesignData = tuple_Heating.Item3;
                        heatingIndex = tuple_Heating.Item2;
                    }

                    result[keyValuePair_Cooling.Key] = new Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>(coolingDesignData, coolingLoad, coolingIndex, heatingDesignData, heatingLoad, heatingIndex);
                }
            }

            if (dictionary_DesignDay_Heating != null)
            {
                foreach (KeyValuePair<string, Tuple<double, int, HeatingDesignData>> keyValuePair_Heating in dictionary_DesignDay_Heating)
                {
                    if (result.ContainsKey(keyValuePair_Heating.Key))
                    {
                        continue;
                    }

                    double coolingLoad = double.NaN;
                    int coolingIndex = -1;
                    CoolingDesignData coolingDesignData = null;
                    double heatingLoad = keyValuePair_Heating.Value.Item1;
                    HeatingDesignData heatingDesignData = keyValuePair_Heating.Value.Item3;
                    int heatingIndex = keyValuePair_Heating.Value.Item2;

                    result[keyValuePair_Heating.Key] = new Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>(coolingDesignData, coolingLoad, coolingIndex, heatingDesignData, heatingLoad, heatingIndex);
                }
            }


            return result;
        }
    }
}