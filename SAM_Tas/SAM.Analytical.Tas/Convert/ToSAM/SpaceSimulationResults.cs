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

                SizingMethod sizingMethod = SizingMethod.Undefined;

                string name = zoneData_BuildingData.name;
                string reference = zoneData_BuildingData.zoneGUID;
                double area = zoneData_BuildingData.floorArea;
                double volume = zoneData_BuildingData.volume;

                SpaceSimulationResult spaceSimulationResult_Cooling = null;
                SpaceSimulationResult spaceSimulationResult_Heating = null;

                //COOLING

                if ((float)values_BuildingData_Cooling[1, i] == 0 && (float)values_CoolingDesignData[1, i] == 0)
                {
                    spaceSimulationResult_Cooling = Analytical.Create.SpaceSimulationResult(name, reference, volume, area, coolingLoad: 0);
                }
                else
                {

                    int index_ZoneData_Cooling = -1;
                    ZoneData zoneData_Cooling = null;

                    if ((float)values_BuildingData_Cooling[1, i] > (float)values_CoolingDesignData[1, i])
                    {
                        sizingMethod = SizingMethod.Simulation;
                        index_ZoneData_Cooling = (int)values_BuildingData_Cooling[2, i];
                        zoneData_Cooling = zoneData_BuildingData;
                    }
                    else
                    {
                        sizingMethod = SizingMethod.CDD;
                        index_ZoneData_Cooling = (int)values_CoolingDesignData[2, i];
                        zoneData_Cooling = coolingDesignData.GetZoneData(i + 1);
                    }

                    if (index_ZoneData_Cooling > -1)
                    {
                        float dryBulbTemp = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.dryBulbTemp);
                        float resultantTemp = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.resultantTemp);
                        float coolingLoad = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.coolingLoad);
                        float solarGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.solarGain);
                        float lightingGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.lightingGain);
                        float infVentGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.infVentGain);
                        float airMovementGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.airMovementGain);
                        float buildingHeatTransfer = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.buildingHeatTransfer);
                        float externalConductionGlazing = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.externalConductionGlazing);
                        float externalConductionOpaque = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.externalConductionOpaque);
                        float occupancySensibleGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.occupantSensibleGain);
                        float equipmentSensibleGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.equipmentSensibleGain);
                        float equipmentLatentGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.equipmentLatentGain);
                        float occupancyLatentGain = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.occupancyLatentGain);
                        float spaceHumidityRatio = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.humidityRatio);
                        float relativeHumidity = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.relativeHumidity);
                        float zoneApertureFlowIn = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.zoneApertureFlowIn);
                        float zoneApertureFlowOut = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.zoneApertureFlowOut);
                        float pollutant = zoneData_Cooling.GetHourlyZoneResult(index_ZoneData_Cooling, (short)tsdZoneArray.pollutant);

                        spaceSimulationResult_Cooling = Analytical.Create.SpaceSimulationResult(name, reference, volume, area, sizingMethod, 
                            dryBulbTemp, resultantTemp, coolingLoad, double.NaN, solarGain, lightingGain, infVentGain, airMovementGain, 
                            buildingHeatTransfer, externalConductionGlazing, externalConductionOpaque, occupancySensibleGain,
                            occupancyLatentGain, equipmentSensibleGain, equipmentLatentGain, spaceHumidityRatio, relativeHumidity,
                            zoneApertureFlowIn, zoneApertureFlowOut, pollutant);
                    }
                }

                //HEATING
                if ((float)values_BuildingData_Heating[1, i] == 0 && (float)values_HeatingDesignData[1, i] == 0)
                {
                    ZoneData zoneData_Heating = heatingDesignData.GetZoneData(i + 1);
                    float dryBulbTemp = zoneData_Heating.GetHourlyZoneResult(1, (short)tsdZoneArray.dryBulbTemp);
                    float resultantTemp = zoneData_Heating.GetHourlyZoneResult(1, (short)tsdZoneArray.resultantTemp);
                    spaceSimulationResult_Heating = Analytical.Create.SpaceSimulationResult(name, reference, volume, area, null, dryBulbTemp, resultantTemp, heatingLoad: 0);
                }
                else
                {
                    int index_ZoneData_Heating = -1;
                    ZoneData zoneData_Heating = null;

                    if ((float)values_BuildingData_Heating[1, i] > (float)values_HeatingDesignData[1, i])
                    {
                        sizingMethod = SizingMethod.Simulation;
                        index_ZoneData_Heating = (int)values_BuildingData_Heating[2, i];
                        zoneData_Heating = zoneDatas_BuildingData[i];
                    }
                    else
                    {
                        sizingMethod = SizingMethod.HDD;
                        index_ZoneData_Heating = (int)values_HeatingDesignData[2, i];
                        zoneData_Heating = heatingDesignData.GetZoneData(i + 1);
                    }

                    if (index_ZoneData_Heating > -1)
                    {
                        float dryBulbTemp = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.dryBulbTemp);
                        float resultantTemp = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.resultantTemp);
                        float heatingLoad = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.heatingLoad);
                        float infVentGain = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.infVentGain);
                        float airMovementGain = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.airMovementGain);
                        float buildingHeatTransfer = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.buildingHeatTransfer);
                        float externalConductionGlazing = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.externalConductionGlazing);
                        float externalConductionOpaque = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.externalConductionOpaque);
                        float spaceHumidityRatio = zoneData_Heating.GetHourlyZoneResult(index_ZoneData_Heating, (short)tsdZoneArray.humidityRatio);

                        spaceSimulationResult_Heating = Analytical.Create.SpaceSimulationResult(name, reference, volume, area, sizingMethod, dryBulbTemp, resultantTemp, 
                            heatingLoad: heatingLoad, 
                            infiltartionGain: infVentGain, 
                            airMovementGain: airMovementGain, 
                            buildingHeatTransfer: buildingHeatTransfer,
                            glazingExternalConduction: externalConductionGlazing,
                            opaqueExternalConduction: externalConductionOpaque,
                            humidityRatio: spaceHumidityRatio);
                    }

                }

                if(spaceSimulationResult_Cooling != null || spaceSimulationResult_Heating != null)
                {
                    Dictionary<SpaceSimulationResultParameter, object> dictionary = Query.Overheating(zoneData_BuildingData, simulationData.firstDay, simulationData.lastDay);

                    if (spaceSimulationResult_Cooling != null)
                    {
                        foreach (KeyValuePair<SpaceSimulationResultParameter, object> keyValuePair in dictionary)
                            spaceSimulationResult_Cooling.SetValue(keyValuePair.Key, keyValuePair.Value);

                        spaceSimulationResult_Cooling.SetValue(SpaceSimulationResultParameter.SimulationType, SimulationType.Cooling.Text());

                        result.Add(spaceSimulationResult_Cooling);
                    }

                    if (spaceSimulationResult_Heating != null)
                    {
                        foreach (KeyValuePair<SpaceSimulationResultParameter, object> keyValuePair in dictionary)
                            spaceSimulationResult_Heating.SetValue(keyValuePair.Key, keyValuePair.Value);

                        spaceSimulationResult_Cooling.SetValue(SpaceSimulationResultParameter.SimulationType, SimulationType.Heating.Text());

                        result.Add(spaceSimulationResult_Heating);
                    }
                }


            }

            return result;
        }
    }
}
