using SAM.Core.Tas;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<SpaceSimulationResult> ToSAM(string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD) || !System.IO.File.Exists(path_TSD))
                return null;

            List<SpaceSimulationResult> result = null;

            using (SAMTSDDocument sAMTBDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = ToSAM(sAMTBDDocument);
            }

            return result;
        }

        public static List<SpaceSimulationResult> ToSAM(this SAMTSDDocument sAMTSDDocument)
        {
            if (sAMTSDDocument == null)
                return null;

            return ToSAM(sAMTSDDocument.TSDDocument?.SimulationData);
        }

        public static List<SpaceSimulationResult> ToSAM(SimulationData simulationData)
        {
            if (simulationData == null)
                return null;

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

            List<SpaceSimulationResult> result = new List<SpaceSimulationResult>();

            for (int i = 0; i < zoneDatas_BuildingData.Count; i++)
            {
                ZoneData zoneData_BuildingData = zoneDatas_BuildingData[i];
                if (zoneDatas_BuildingData == null)
                    continue;

                float load_Simulation = float.NaN;
                int index_Simulation = -1;
                ZoneData zoneData_Simulation = null;
                float load_DesignDay = float.NaN;
                int index_DesignDay = -1;
                ZoneData zoneData_DesignDay = null;

                load_Simulation = (float)values_BuildingData_Cooling[1, i];
                load_DesignDay = (float)values_CoolingDesignData[1, i];
                index_Simulation = (int)values_BuildingData_Cooling[2, i];
                index_DesignDay = (int)values_CoolingDesignData[2, i];
                zoneData_Simulation = zoneData_BuildingData;
                zoneData_DesignDay = coolingDesignData.GetZoneData(i + 1);

                SpaceSimulationResult spaceSimulationResult_Cooling = Create.SpaceSimulationResult(LoadType.Cooling, load_Simulation, index_Simulation, zoneData_Simulation, load_DesignDay, index_DesignDay, zoneData_DesignDay);

                load_Simulation = (float)values_BuildingData_Heating[1, i];
                load_DesignDay = (float)values_HeatingDesignData[1, i];
                index_Simulation = (int)values_BuildingData_Heating[2, i];
                index_DesignDay = (int)values_HeatingDesignData[2, i];
                zoneData_Simulation = zoneData_BuildingData;
                zoneData_DesignDay = heatingDesignData.GetZoneData(i + 1);

                SpaceSimulationResult spaceSimulationResult_Heating = Create.SpaceSimulationResult(LoadType.Heating, load_Simulation, index_Simulation, zoneData_Simulation, load_DesignDay, index_DesignDay, zoneData_DesignDay);

                if(spaceSimulationResult_Cooling != null || spaceSimulationResult_Heating != null)
                {
                    Dictionary<SpaceSimulationResultParameter, object> dictionary = Query.Overheating(zoneData_BuildingData, simulationData.firstDay, simulationData.lastDay);

                    if (spaceSimulationResult_Cooling != null)
                    {
                        foreach (KeyValuePair<SpaceSimulationResultParameter, object> keyValuePair in dictionary)
                            spaceSimulationResult_Cooling.SetValue(keyValuePair.Key, keyValuePair.Value);

                        result.Add(spaceSimulationResult_Cooling);
                    }

                    if (spaceSimulationResult_Heating != null)
                    {
                        foreach (KeyValuePair<SpaceSimulationResultParameter, object> keyValuePair in dictionary)
                            spaceSimulationResult_Heating.SetValue(keyValuePair.Key, keyValuePair.Value);

                        result.Add(spaceSimulationResult_Heating);
                    }
                }
            }

            return result;
        }
    }
}
