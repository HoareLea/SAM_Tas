using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<Core.Result> ToSAM_Results(string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
                return null;

            List<Core.Result> result = null;

            using (SAMTSDDocument sAMTBDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = ToSAM_Results(sAMTBDDocument);
            }

            return result;
        }

        public static List<Core.Result> ToSAM_Results(this SAMTSDDocument sAMTSDDocument)
        {
            if (sAMTSDDocument == null)
                return null;

            return ToSAM_Results(sAMTSDDocument.TSDDocument?.SimulationData);
        }

        //Pull/Convert data for Spaces (in Tas they call them Zones) but not for SAM Zones (in Tas ZoneGroups)
        public static List<Core.Result> ToSAM_Results(SimulationData simulationData)
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

            Dictionary<string, Tuple<double, int>> dictionary_Cooling = Query.ValueDictionary(buildingData, tsdZoneArray.coolingLoad);
            Dictionary<string, Tuple<double, int>> dictionary_Heating = Query.ValueDictionary(buildingData, tsdZoneArray.heatingLoad);

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

                double designDayTemperature;
                double designDayRelativeHumidity;

                SizingMethod sizingMethod;

                //COOLING START

                Tuple<double, int> tuple_Cooling = null;
                if (dictionary_Cooling == null || !dictionary_Cooling.TryGetValue(keyValuePair.Key, out tuple_Cooling))
                {
                    tuple_Cooling = null;
                }

                sizingMethod = SizingMethod.Undefined;
                ZoneData zoneData_Cooling = null;
                double coolingLoad = double.NaN;
                int coolingIndex = -1;
                designDayTemperature = double.NaN;
                designDayRelativeHumidity = double.NaN;

                CoolingDesignData coolingDesignData = keyValuePair.Value.Item1;
                if (coolingDesignData != null)
                {
                    sizingMethod = SizingMethod.CDD;
                    zoneData_Cooling = coolingDesignData.GetZoneData(zoneData_BuildingData.zoneNumber);
                    coolingLoad = keyValuePair.Value.Item2;
                    coolingIndex = keyValuePair.Value.Item3;

                    //TODO: Add Design Day Temperature and Design Day Relative Humidity for CDD sizing method
                }

                if (tuple_Cooling != null && tuple_Cooling.Item1 > coolingLoad)
                {
                    sizingMethod = SizingMethod.Simulation;
                    zoneData_Cooling = zoneData_BuildingData;
                    coolingLoad = tuple_Cooling.Item1;
                    coolingIndex = tuple_Cooling.Item2;
                    designDayTemperature = buildingData.GetHourlyBuildingResult(coolingIndex, (int)tsdBuildingArray.externalTemperature);
                    designDayRelativeHumidity = buildingData.GetHourlyBuildingResult(coolingIndex, (int)tsdBuildingArray.externalHumidity);
                }

                SpaceSimulationResult spaceSimulationResult_Cooling = Create.SpaceSimulationResult(zoneData_Cooling, coolingIndex, LoadType.Cooling, sizingMethod);
                if(spaceSimulationResult_Cooling != null && coolingDesignData != null)
                {
                    string designDayName = coolingDesignData.name;
                    spaceSimulationResult_Cooling.SetValue(SpaceSimulationResultParameter.DesignDayName, designDayName);
                }

                if(!double.IsNaN(designDayTemperature))
                {
                    spaceSimulationResult_Cooling.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayTemperature, designDayTemperature);
                }

                if (!double.IsNaN(designDayRelativeHumidity))
                {
                    spaceSimulationResult_Cooling.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayRelativeHumidity, designDayRelativeHumidity);
                }

                //COOLING END

                //HEATING START

                Tuple<double, int> tuple_Heating = null;
                if (dictionary_Heating == null || !dictionary_Heating.TryGetValue(keyValuePair.Key, out tuple_Heating))
                {
                    tuple_Heating = null;
                }

                sizingMethod = SizingMethod.Undefined;
                ZoneData zoneData_Heating = null;
                double heatingLoad = double.NaN;
                int heatingIndex = -1;
                designDayTemperature = double.NaN;
                designDayRelativeHumidity = double.NaN;

                HeatingDesignData heatingDesignData = keyValuePair.Value.Item4;
                if(heatingDesignData != null)
                {
                    sizingMethod = SizingMethod.HDD;
                    zoneData_Heating = heatingDesignData.GetZoneData(zoneData_BuildingData.zoneNumber);
                    heatingLoad = keyValuePair.Value.Item5;
                    heatingIndex = keyValuePair.Value.Item6;

                    //TODO: Add Design Day Temperature and Design Day Relative Humidity for CDD sizing method
                }

                if (tuple_Heating != null && tuple_Heating.Item1 > heatingLoad)
                {
                    sizingMethod = SizingMethod.Simulation;
                    zoneData_Cooling = zoneData_BuildingData;
                    coolingLoad = tuple_Heating.Item1;
                    coolingIndex = tuple_Heating.Item2;
                    designDayTemperature = buildingData.GetHourlyBuildingResult(coolingIndex, (int)tsdBuildingArray.externalTemperature);
                    designDayRelativeHumidity = buildingData.GetHourlyBuildingResult(coolingIndex, (int)tsdBuildingArray.externalHumidity);
                }

                SpaceSimulationResult spaceSimulationResult_Heating = Create.SpaceSimulationResult(zoneData_Heating, heatingIndex, LoadType.Heating, sizingMethod);
                if (spaceSimulationResult_Heating != null && heatingDesignData != null)
                {
                    string designDayName = heatingDesignData.name;
                    spaceSimulationResult_Heating.SetValue(SpaceSimulationResultParameter.DesignDayName, designDayName);
                }

                if (!double.IsNaN(designDayTemperature))
                {
                    spaceSimulationResult_Heating.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayTemperature, designDayTemperature);
                }

                if (!double.IsNaN(designDayRelativeHumidity))
                {
                    spaceSimulationResult_Heating.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayRelativeHumidity, designDayRelativeHumidity);
                }

                //HEATING END

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

                    List<SurfaceSimulationResult> surfaceSimulationResults = zoneData_Cooling.ToSAM_SurfaceSimulationResults(loadIndex);
                    if (surfaceSimulationResults == null)
                    {
                        continue;
                    }

                    foreach (SurfaceSimulationResult surfaceSimulationResult in surfaceSimulationResults)
                    {
                        surfaceSimulationResult.SetValue(Analytical.SurfaceSimulationResultParameter.LoadType, LoadType.Cooling.ToString());
                        results[index].Add(surfaceSimulationResult);
                    }
                }

                if (spaceSimulationResult_Heating != null)
                {
                    if (!spaceSimulationResult_Heating.TryGetValue(Analytical.SpaceSimulationResultParameter.LoadIndex, out int loadIndex))
                    {
                        continue;
                    }

                    List<SurfaceSimulationResult> surfaceSimulationResults = zoneData_Heating.ToSAM_SurfaceSimulationResults(loadIndex);
                    if (surfaceSimulationResults == null)
                    {
                        continue;
                    }

                    foreach (SurfaceSimulationResult surfaceSimulationResult in surfaceSimulationResults)
                    {
                        surfaceSimulationResult.SetValue(Analytical.SurfaceSimulationResultParameter.LoadType, LoadType.Heating.ToString());
                        results[index].Add(surfaceSimulationResult);
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
