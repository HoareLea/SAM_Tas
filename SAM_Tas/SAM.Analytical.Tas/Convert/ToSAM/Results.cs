using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (simulationData == null)
                return null;

            //buildingData is is yearly dynamic simulation data
            BuildingData buildingData = simulationData.GetBuildingData();

            List<ZoneData> zoneDatas_BuildingData = Query.ZoneDatas(buildingData);
            if (zoneDatas_BuildingData == null || zoneDatas_BuildingData.Count == 0)
                return null;

            CoolingDesignData coolingDesignData = simulationData.GetCoolingDesignData(0);
            HeatingDesignData heatingDesignData = simulationData.GetHeatingDesignData(0);

            //COOLING
            object[,] values_BuildingData_Cooling = buildingData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray.coolingLoad });
            object[,] values_CoolingDesignData = coolingDesignData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray.coolingLoad });

            //HEATING
            object[,] values_BuildingData_Heating = buildingData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray.heatingLoad });
            object[,] values_HeatingDesignData = heatingDesignData.GetPeakZoneGains(new short[1] { (short)tsdZoneArray.heatingLoad });

            //in SpaceSimulationResult we stored data from tas that is alreay pull in correct output format so Heating is from HDD and cooling is max of design and dynamic simulation
            List<List<Core.Result>> results = Enumerable.Repeat<List<Core.Result>>(null, zoneDatas_BuildingData.Count).ToList();
            //Parallel.For(0, zoneDatas_BuildingData.Count, (int i) =>
            for (int i = 0; i < zoneDatas_BuildingData.Count; i++)
            {
                ZoneData zoneData_BuildingData = zoneDatas_BuildingData[i];
                if (zoneDatas_BuildingData == null)
                {
                    //return;
                    continue;
                }

                float load_Simulation = float.NaN;
                int index_Simulation = -1;
                float load_DesignDay = float.NaN;
                int index_DesignDay = -1;
                //ZoneData zoneData_DesignDay = null;

                ZoneData zoneData_Simulation = zoneData_BuildingData;

                load_Simulation = (float)values_BuildingData_Cooling[1, i];
                load_DesignDay = (float)values_CoolingDesignData[1, i];
                index_Simulation = (int)values_BuildingData_Cooling[2, i];
                index_DesignDay = (int)values_CoolingDesignData[2, i];
                
                ZoneData zoneData_DesignDay_Cooling = coolingDesignData.GetZoneData(i + 1);

                SpaceSimulationResult spaceSimulationResult_Cooling = Create.SpaceSimulationResult(LoadType.Cooling, load_Simulation, index_Simulation, zoneData_Simulation, load_DesignDay, index_DesignDay, zoneData_DesignDay_Cooling);

                load_Simulation = (float)values_BuildingData_Heating[1, i];
                load_DesignDay = (float)values_HeatingDesignData[1, i];
                index_Simulation = (int)values_BuildingData_Heating[2, i];
                index_DesignDay = (int)values_HeatingDesignData[2, i];

                ZoneData zoneData_DesignDay_Heating = heatingDesignData.GetZoneData(i + 1);

                //Here we make sure that HDD will be chose if small difference is between two cases
                SpaceSimulationResult spaceSimulationResult_Heating = null;
                if (System.Math.Abs(load_Simulation - load_DesignDay) < 10)
                    spaceSimulationResult_Heating = Create.SpaceSimulationResult(zoneData_DesignDay_Heating, index_DesignDay, LoadType.Heating, SizingMethod.HDD); //DesignDay
                else
                    spaceSimulationResult_Heating = Create.SpaceSimulationResult(LoadType.Heating, load_Simulation, index_Simulation, zoneData_Simulation, load_DesignDay, index_DesignDay, zoneData_DesignDay_Heating);

                if (spaceSimulationResult_Cooling != null || spaceSimulationResult_Heating != null)
                {
                    Dictionary<SpaceSimulationResultParameter, object> dictionary = Query.Overheating(zoneData_BuildingData, simulationData.firstDay, simulationData.lastDay);

                    results[i] = new List<Core.Result>();

                    if (spaceSimulationResult_Cooling != null)
                    {
                        foreach (KeyValuePair<SpaceSimulationResultParameter, object> keyValuePair in dictionary)
                            spaceSimulationResult_Cooling.SetValue(keyValuePair.Key, keyValuePair.Value);

                        results[i].Add(spaceSimulationResult_Cooling);
                    }

                    if (spaceSimulationResult_Heating != null)
                    {
                        foreach (KeyValuePair<SpaceSimulationResultParameter, object> keyValuePair in dictionary)
                            spaceSimulationResult_Heating.SetValue(keyValuePair.Key, keyValuePair.Value);

                        results[i].Add(spaceSimulationResult_Heating);
                    }
                }


                if (spaceSimulationResult_Cooling != null)
                {
                    ZoneData zoneData = spaceSimulationResult_Cooling.SizingMethod() == SizingMethod.Simulation ? zoneData_Simulation : zoneData_DesignDay_Cooling;

                    if(!spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.LoadIndex, out int index))
                    {
                        continue;
                    }

                    List<PanelSimulationResult> panelSimulationResults = zoneData.ToSAM_PanelSimulationResults(index);
                    if(panelSimulationResults == null)
                    {
                        continue;
                    }

                    foreach(PanelSimulationResult panelSimulationResult in panelSimulationResults)
                    {
                        panelSimulationResult.SetValue(Analytical.PanelSimulationResultParameter.LoadType, LoadType.Cooling.ToString());
                        results[i].Add(panelSimulationResult);
                    }
                }

                if (spaceSimulationResult_Heating != null)
                {
                    ZoneData zoneData = spaceSimulationResult_Heating.SizingMethod() == SizingMethod.Simulation ? zoneData_Simulation : zoneData_DesignDay_Heating;

                    if (!spaceSimulationResult_Cooling.TryGetValue(SpaceSimulationResultParameter.LoadIndex, out int index))
                    {
                        continue;
                    }

                    List<PanelSimulationResult> panelSimulationResults = zoneData.ToSAM_PanelSimulationResults(index);
                    if (panelSimulationResults == null)
                    {
                        continue;
                    }

                    foreach (PanelSimulationResult panelSimulationResult in panelSimulationResults)
                    {
                        panelSimulationResult.SetValue(Analytical.PanelSimulationResultParameter.LoadType, LoadType.Heating.ToString());
                        results[i].Add(panelSimulationResult);
                    }
                }
            };//);

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
