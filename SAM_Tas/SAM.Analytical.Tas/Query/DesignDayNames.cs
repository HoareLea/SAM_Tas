using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<Tuple<string, string, string>> DesignDayNames(this string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return null;
            }

            List<Tuple<string, string, string>> result = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD))
            {
                result = DesignDayNames(sAMTSDDocument);
            }

            return result;
        }

        public static List<Tuple<string, string, string>> DesignDayNames(this SAMTSDDocument sAMTSDDocument)
        {
            SimulationData simulationData = sAMTSDDocument?.TSDDocument?.SimulationData;
            if (simulationData == null)
            {
                return null;
            }

            BuildingData buildingData = simulationData.GetBuildingData();

            List<ZoneData> zoneDatas_BuildingData = ZoneDatas(buildingData);
            if (zoneDatas_BuildingData == null || zoneDatas_BuildingData.Count == 0)
                return null;

            object[,] values_CoolingBuildingData = buildingData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray.coolingLoad });
            object[,] values_CoolingDesignData = simulationData.GetCoolingDesignData(0).GetPeakZoneGains(new short[1] { (short)tsdZoneArray.coolingLoad });

            object[,] values_HeatingBuildingData = buildingData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray.heatingLoad });
            object[,] values_HeatingDesignData = simulationData.GetHeatingDesignData(0).GetPeakZoneGains(new short[1] { (short)tsdZoneArray.heatingLoad });

            List<Tuple<string, double, double>> tuples_Load = new List<Tuple<string, double, double>>();
            for (int i = 0; i < zoneDatas_BuildingData.Count; i++)
            {
                ZoneData zoneData_BuildingData = zoneDatas_BuildingData[i];
                if (zoneDatas_BuildingData == null)
                {
                    continue;
                }

                string id = zoneData_BuildingData.zoneGUID;

                double coolingLoad_Simulation = (float)values_CoolingBuildingData[1, i];
                double coolingLoad_DesignDay = (float)values_CoolingDesignData[1, i];

                if (coolingLoad_Simulation < coolingLoad_DesignDay)
                {
                    coolingLoad_DesignDay = double.NaN;
                }

                double heatingLoad_Simulation = (float)values_HeatingBuildingData[1, i];
                double heatingLoad_DesignDay = (float)values_HeatingDesignData[1, i];

                if (heatingLoad_Simulation < heatingLoad_DesignDay)
                {
                    heatingLoad_DesignDay = double.NaN;
                }

                tuples_Load.Add(new Tuple<string, double, double>(id, heatingLoad_DesignDay, coolingLoad_DesignDay));

            }

            Dictionary<string, string> dictionary_Cooling = new Dictionary<string, string>();

            List<CoolingDesignData> coolingDesignDatas = simulationData.CoolingDesignDatas();
            if (coolingDesignDatas != null)
            {
                foreach (CoolingDesignData coolingDesignData in coolingDesignDatas)
                {
                    List<ZoneData> zoneDatas = coolingDesignData.ZoneDatas();
                    if (zoneDatas != null && zoneDatas.Count != 0)
                    {
                        foreach (ZoneData zoneData in zoneDatas)
                        {
                            string id = zoneData.zoneGUID;
                            int index = -1;

                            index = tuples_Load.FindIndex(x => x.Item1 == id);
                            if(index == -1)
                            {
                                continue;
                            }

                            if(tuples_Load[index].Item3 != (float)zoneData.GetPeakZoneGain(tsdZoneArray.coolingLoad))
                            {
                                continue;
                            }

                            dictionary_Cooling[id] = coolingDesignData.name;
                        }
                    }
                }
            }

            Dictionary<string, string> dictionary_Heating = new Dictionary<string, string>();

            List<HeatingDesignData> heatingDesignDatas = simulationData.HeatingDesignDatas();
            if (heatingDesignDatas != null)
            {
                foreach (HeatingDesignData heatingDesignData in heatingDesignDatas)
                {
                    List<ZoneData> zoneDatas = heatingDesignData.ZoneDatas();
                    if (zoneDatas != null && zoneDatas.Count != 0)
                    {
                        foreach (ZoneData zoneData in zoneDatas)
                        {
                            string id = zoneData.zoneGUID;
                            int index = -1;

                            index = tuples_Load.FindIndex(x => x.Item1 == id);
                            if (index == -1)
                            {
                                continue;
                            }

                            if (tuples_Load[index].Item3 != (float)zoneData.GetPeakZoneGain(tsdZoneArray.heatingLoad))
                            {
                                continue;
                            }

                            dictionary_Heating[id] = heatingDesignData.name;
                        }
                    }
                }
            }

            List<Tuple<string, string, string>> result = new List<Tuple<string, string, string>>();
            
            if (dictionary_Heating != null)
            {
                foreach(KeyValuePair<string, string> keyValuePair in dictionary_Heating)
                {
                    string designDay_Cooling = null;

                    dictionary_Cooling?.TryGetValue(keyValuePair.Key, out designDay_Cooling);

                    result.Add(new Tuple<string, string, string>(keyValuePair.Key, keyValuePair.Value, designDay_Cooling));
                }
            }

            if(dictionary_Cooling != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in dictionary_Cooling)
                {
                    result.Add(new Tuple<string, string, string>(keyValuePair.Key, null, keyValuePair.Value));
                }
            }

            return result;
        }
    }
}