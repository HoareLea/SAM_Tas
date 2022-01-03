using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<Core.Result> ToSAM(string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
                return null;

            List<Core.Result> result = null;

            using (SAMTSDDocument sAMTBDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = ToSAM(sAMTBDDocument);
            }

            return result;
        }

        public static List<Core.Result> ToSAM(this SAMTSDDocument sAMTSDDocument)
        {
            if (sAMTSDDocument == null)
                return null;

            return ToSAM(sAMTSDDocument.TSDDocument?.SimulationData);
        }

        //Pull/Convert data for Spaces (in Tas they call them Zones) but not for SAM Zones (in Tas ZoneGroups)
        public static List<Core.Result> ToSAM(SimulationData simulationData)
        {
            //buildingData is is yearly dynamic simulation data
            BuildingData buildingData = simulationData?.GetBuildingData();
            if(buildingData == null)
            {
                return null;
            }

            List<ZoneData> zoneDatas = Query.ZoneDatas(buildingData);
            if (zoneDatas == null || zoneDatas.Count == 0)
            {
                return null;
            }

            Dictionary<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> designDataDictionary = Query.DesignDataDictionary(simulationData);

            List<List<Core.Result>> results = Enumerable.Repeat<List<Core.Result>>(null, zoneDatas.Count).ToList();
            foreach (KeyValuePair<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> keyValuePair in designDataDictionary)
            {
                int index = zoneDatas.FindIndex(x => x.zoneGUID == keyValuePair.Key);
                if(index == -1)
                {
                    continue;
                }
                
                ZoneData zoneData_BuildingData = zoneDatas[index];
                if(zoneData_BuildingData == null)
                {
                    continue;
                }

                SizingMethod sizingMethod = SizingMethod.Undefined;

                ZoneData zoneData_Cooling = zoneData_BuildingData;

                CoolingDesignData coolingDesignData = keyValuePair.Value.Item1;
                double coolingLoad = keyValuePair.Value.Item2;
                int coolingIndex = keyValuePair.Value.Item3;
                if(coolingDesignData != null)
                {
                    sizingMethod = SizingMethod.CDD;
                    zoneData_Cooling = coolingDesignData.GetZoneData(zoneData_BuildingData.zoneNumber);
                }
                else
                {
                    sizingMethod = SizingMethod.Simulation;
                }

                ZoneData zoneData_Heating = zoneData_BuildingData;

                SpaceSimulationResult spaceSimulationResult_Cooling = Create.SpaceSimulationResult(zoneData_BuildingData, coolingIndex, LoadType.Cooling, sizingMethod);
                if(spaceSimulationResult_Cooling != null && coolingDesignData != null)
                {
                    spaceSimulationResult_Cooling.SetValue(SpaceSimulationResultParameter.CoolingDesignDayName, coolingDesignData.name);
                }

                HeatingDesignData heatingDesignData = keyValuePair.Value.Item4;
                double heatingLoad = keyValuePair.Value.Item5;
                int heatingIndex = keyValuePair.Value.Item6;
                if (heatingDesignData != null)
                {
                    sizingMethod = SizingMethod.HDD;
                    zoneData_Heating = heatingDesignData.GetZoneData(zoneData_BuildingData.zoneNumber);
                }
                else
                {
                    sizingMethod = SizingMethod.Simulation;
                }

                SpaceSimulationResult spaceSimulationResult_Heating = Create.SpaceSimulationResult(zoneData_Heating, heatingIndex, LoadType.Heating, sizingMethod);
                if (spaceSimulationResult_Heating != null && heatingDesignData != null)
                {
                    spaceSimulationResult_Heating.SetValue(SpaceSimulationResultParameter.HeatingDesignDayName, heatingDesignData.name);
                }

                if (spaceSimulationResult_Cooling != null || spaceSimulationResult_Heating != null)
                {
                    Dictionary<Analytical.SpaceSimulationResultParameter, object> dictionary = Query.Overheating(zoneData_BuildingData, simulationData.firstDay, simulationData.lastDay);

                    results[index] = new List<Core.Result>();

                    if (spaceSimulationResult_Cooling != null)
                    {
                        foreach (KeyValuePair<Analytical.SpaceSimulationResultParameter, object> keyValuePair_Temp in dictionary)
                            spaceSimulationResult_Cooling.SetValue(keyValuePair_Temp.Key, keyValuePair_Temp.Value);

                        results[index].Add(spaceSimulationResult_Cooling);
                    }

                    if (spaceSimulationResult_Heating != null)
                    {
                        foreach (KeyValuePair<Analytical.SpaceSimulationResultParameter, object> keyValuePair_Temp in dictionary)
                            spaceSimulationResult_Heating.SetValue(keyValuePair_Temp.Key, keyValuePair_Temp.Value);

                        results[index].Add(spaceSimulationResult_Heating);
                    }
                }

                if (spaceSimulationResult_Cooling != null)
                {
                    if (!spaceSimulationResult_Cooling.TryGetValue(Analytical.SpaceSimulationResultParameter.LoadIndex, out int loadIndex))
                    {
                        continue;
                    }

                    List<PanelSimulationResult> panelSimulationResults = zoneData_Cooling.ToSAM_PanelSimulationResults(loadIndex);
                    if (panelSimulationResults == null)
                    {
                        continue;
                    }

                    foreach (PanelSimulationResult panelSimulationResult in panelSimulationResults)
                    {
                        panelSimulationResult.SetValue(Analytical.PanelSimulationResultParameter.LoadType, LoadType.Cooling.ToString());
                        results[index].Add(panelSimulationResult);
                    }
                }

                if (spaceSimulationResult_Heating != null)
                {
                    if (!spaceSimulationResult_Heating.TryGetValue(Analytical.SpaceSimulationResultParameter.LoadIndex, out int loadIndex))
                    {
                        continue;
                    }

                    List<PanelSimulationResult> panelSimulationResults = zoneData_Heating.ToSAM_PanelSimulationResults(loadIndex);
                    if (panelSimulationResults == null)
                    {
                        continue;
                    }

                    foreach (PanelSimulationResult panelSimulationResult in panelSimulationResults)
                    {
                        panelSimulationResult.SetValue(Analytical.PanelSimulationResultParameter.LoadType, LoadType.Heating.ToString());
                        results[index].Add(panelSimulationResult);
                    }
                }
            }

            List<Core.Result> result = new List<Core.Result>();
            foreach (List<Core.Result> spaceSimulationResults_Temp in results)
            {
                if (spaceSimulationResults_Temp != null)
                {
                    result.AddRange(spaceSimulationResults_Temp);
                }
            }

            return result;
        }
    }
}
