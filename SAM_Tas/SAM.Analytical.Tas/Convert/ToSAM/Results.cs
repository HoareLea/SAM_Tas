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
        //
        //Per space and load type the TSD holds two peaks: the design-day run's and the full-year simulation's. Both
        //are persisted, separately and never merged, as Analytical.SpaceSimulationResultParameter.DesignDayPeak and
        //AnnualPeak (SpaceLoadPeak: 0-based time, no sentinels, Tas-signed heat-balance terms). The legacy values
        //(Load, LoadIndex, SizingMethod, room state, gains, DesignDayTemperature/RelativeHumidity) stay a projection
        //of the GOVERNING peak, as before: the design day unless the annual peak is strictly larger. LoadIndex keeps
        //the raw 1-based Tas index, and a zero governing peak keeps the -1 values Tas returns for index 0.
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
            for (int index = 0; index < zoneDatas.Count; index++)
            {
                ZoneData zoneData_BuildingData = zoneDatas[index];
                if(zoneData_BuildingData == null)
                {
                    continue;
                }

                string zoneGuid = zoneData_BuildingData.zoneGUID;

                Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int> tuple_DesignData = null;
                if (designDataDictionary == null || zoneGuid == null || !designDataDictionary.TryGetValue(zoneGuid, out tuple_DesignData))
                {
                    tuple_DesignData = null;
                }

                Tuple<double, int> tuple_Cooling = null;
                if (dictionary_Cooling == null || zoneGuid == null || !dictionary_Cooling.TryGetValue(zoneGuid, out tuple_Cooling))
                {
                    tuple_Cooling = null;
                }

                Tuple<double, int> tuple_Heating = null;
                if (dictionary_Heating == null || zoneGuid == null || !dictionary_Heating.TryGetValue(zoneGuid, out tuple_Heating))
                {
                    tuple_Heating = null;
                }

                //COOLING
                CoolingDesignData coolingDesignData = tuple_DesignData?.Item1;
                SpaceSimulationResult spaceSimulationResult_Cooling = ToSAM_SpaceSimulationResult(LoadType.Cooling, buildingData, zoneData_BuildingData, tuple_Cooling,
                    coolingDesignData?.GetZoneData(zoneData_BuildingData.zoneNumber), coolingDesignData?.name, tuple_DesignData?.Item2 ?? double.NaN, tuple_DesignData?.Item3 ?? -1,
                    out ZoneData zoneData_Cooling);

                //HEATING
                HeatingDesignData heatingDesignData = tuple_DesignData?.Item4;
                SpaceSimulationResult spaceSimulationResult_Heating = ToSAM_SpaceSimulationResult(LoadType.Heating, buildingData, zoneData_BuildingData, tuple_Heating,
                    heatingDesignData?.GetZoneData(zoneData_BuildingData.zoneNumber), heatingDesignData?.name, tuple_DesignData?.Item5 ?? double.NaN, tuple_DesignData?.Item6 ?? -1,
                    out ZoneData zoneData_Heating);

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

        /// <summary>
        /// One space's heating or cooling result from its two TSD peaks, or <c>null</c> when it has neither.
        /// </summary>
        /// <param name="tuple_Annual">The building (annual) data's <c>GetPeakZoneGains</c> peak and 1-based index; null when absent.</param>
        /// <param name="zoneData_DesignDay">The zone in the governing design day's data; null when there is no design day.</param>
        /// <param name="load_DesignDay">The design day's <c>GetPeakZoneGains</c> peak, W.</param>
        /// <param name="index_DesignDay">The design day's 1-based <c>GetPeakZoneGains</c> index.</param>
        /// <param name="zoneData_Governing">The zone data the legacy values (and the surface results) were read from.</param>
        private static SpaceSimulationResult ToSAM_SpaceSimulationResult(LoadType loadType, BuildingData buildingData, ZoneData zoneData_BuildingData, Tuple<double, int> tuple_Annual,
            ZoneData zoneData_DesignDay, string designDayName, double load_DesignDay, int index_DesignDay, out ZoneData zoneData_Governing)
        {
            zoneData_Governing = null;

            bool designDay = zoneData_DesignDay != null;
            bool annual = tuple_Annual != null && zoneData_BuildingData != null;

            //The governing peak, as before: the design day, unless the annual peak is strictly larger. With only one of
            //them, that one. (Before: a missing design day left the space with no result for that load type, and an
            //annual heating winner was written into the cooling variables - audit B1.)
            SizingMethod sizingMethod;
            int index_Governing;
            if (designDay && (!annual || !(tuple_Annual.Item1 > load_DesignDay)))
            {
                sizingMethod = loadType == LoadType.Cooling ? SizingMethod.CDD : SizingMethod.HDD;
                zoneData_Governing = zoneData_DesignDay;
                index_Governing = index_DesignDay;
            }
            else if (annual)
            {
                sizingMethod = SizingMethod.Simulation;
                zoneData_Governing = zoneData_BuildingData;
                index_Governing = tuple_Annual.Item2;
            }
            else
            {
                return null;
            }

            SpaceSimulationResult result = Create.SpaceSimulationResult(zoneData_Governing, index_Governing, loadType, sizingMethod);
            if (result == null)
            {
                return null;
            }

            if (designDay)
            {
                result.SetValue(SpaceSimulationResultParameter.DesignDayName, designDayName);
            }

            //Legacy: the outdoor state at the governing peak, recorded only when the annual peak governs, because the
            //design-day data sets have no building (weather) results.
            if (sizingMethod == SizingMethod.Simulation)
            {
                result.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayTemperature, (double)buildingData.GetHourlyBuildingResult(index_Governing, (int)tsdBuildingArray.externalTemperature));
                result.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayRelativeHumidity, (double)buildingData.GetHourlyBuildingResult(index_Governing, (int)tsdBuildingArray.externalHumidity));
            }

            SpaceLoadPeak spaceLoadPeak_DesignDay = designDay ? Create.SpaceLoadPeak(zoneData_DesignDay, LoadPeakBasis.DesignDay, load_DesignDay, index_DesignDay, null, designDayName) : null;
            if (spaceLoadPeak_DesignDay != null)
            {
                result.SetValue(Analytical.SpaceSimulationResultParameter.DesignDayPeak, spaceLoadPeak_DesignDay);
            }

            SpaceLoadPeak spaceLoadPeak_Annual = annual ? Create.SpaceLoadPeak(zoneData_BuildingData, LoadPeakBasis.AnnualSimulation, tuple_Annual.Item1, tuple_Annual.Item2, buildingData) : null;
            if (spaceLoadPeak_Annual != null)
            {
                result.SetValue(Analytical.SpaceSimulationResultParameter.AnnualPeak, spaceLoadPeak_Annual);
            }

            return result;
        }
    }
}
