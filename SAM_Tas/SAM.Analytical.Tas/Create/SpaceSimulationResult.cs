
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static SpaceSimulationResult SpaceSimulationResult(this ZoneData zoneData, LoadType loadType)
        {
            if (zoneData == null)
                return null;

            string name = zoneData.name;
            string reference = zoneData.zoneGUID;
            double area = zoneData.floorArea;
            double volume = zoneData.volume;

            return Analytical.Create.SpaceSimulationResult(name, reference, volume, area, loadType, 0);
        }

        public static SpaceSimulationResult SpaceSimulationResult(this ZoneData zoneData)
        {
            if (zoneData == null)
                return null;

            string name = zoneData.name;
            string reference = zoneData.zoneGUID;
            double area = zoneData.floorArea;
            double volume = zoneData.volume;

            return Analytical.Create.SpaceSimulationResult(name, reference, volume, area);
        }

        public static SpaceSimulationResult SpaceSimulationResult(this ZoneData zoneData, int index, LoadType loadType, SizingMethod sizingMethod)
        {
            if (zoneData == null || index == -1 || loadType == LoadType.Undefined)
                return null;

            string name = zoneData.name;
            string reference = zoneData.zoneGUID;
            double area = zoneData.floorArea;
            double volume = zoneData.volume;

            float dryBulbTemp = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.dryBulbTemp);
            float resultantTemp = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.resultantTemp);
            float load = zoneData.GetHourlyZoneResult(index, loadType == LoadType.Cooling ? (short)tsdZoneArray.coolingLoad : (short)tsdZoneArray.heatingLoad);
            float infVentGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.infVentGain);
            float airMovementGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.airMovementGain);
            float buildingHeatTransfer = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.buildingHeatTransfer);
            float externalConductionGlazing = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.externalConductionGlazing);
            float externalConductionOpaque = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.externalConductionOpaque);
            float spaceHumidityRatio = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.humidityRatio);
            if (loadType == LoadType.Heating)
                return Analytical.Create.SpaceSimulationResult(name, reference, volume, area, LoadType.Heating, load, index, sizingMethod, dryBulbTemp, resultantTemp,
                    infiltartionGain: infVentGain,
                    airMovementGain: airMovementGain,
                    buildingHeatTransfer: buildingHeatTransfer,
                    glazingExternalConduction: externalConductionGlazing,
                    opaqueExternalConduction: externalConductionOpaque,
                    humidityRatio: spaceHumidityRatio);

            float solarGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.solarGain);
            float lightingGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.lightingGain);
            float occupancySensibleGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.occupantSensibleGain);
            float equipmentSensibleGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.equipmentSensibleGain);
            float equipmentLatentGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.equipmentLatentGain);
            float occupancyLatentGain = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.occupancyLatentGain);
            float relativeHumidity = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.relativeHumidity);
            float zoneApertureFlowIn = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.zoneApertureFlowIn);
            float zoneApertureFlowOut = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.zoneApertureFlowOut);
            float pollutant = zoneData.GetHourlyZoneResult(index, (short)tsdZoneArray.pollutant);


            return Analytical.Create.SpaceSimulationResult(name, reference, volume, area, loadType, load, index, sizingMethod,
                dryBulbTemp, resultantTemp, solarGain, lightingGain, infVentGain, airMovementGain,
                buildingHeatTransfer, externalConductionGlazing, externalConductionOpaque, occupancySensibleGain,
                occupancyLatentGain, equipmentSensibleGain, equipmentLatentGain, spaceHumidityRatio, relativeHumidity,
                zoneApertureFlowIn, zoneApertureFlowOut, pollutant);
        }

        public static SpaceSimulationResult SpaceSimulationResult(LoadType loadType, float load_Simulation, int index_Simulation, ZoneData zoneData_Simulation, float load_DesignDay, int index_DesignDay, ZoneData zoneData_DesignDay)
        {
            if (loadType == LoadType.Undefined || float.IsNaN(load_Simulation) || float.IsNaN(load_DesignDay))
                return null;

            switch(loadType)
            {
                case LoadType.Cooling:
                    return SpaceSimulationResult_Cooling(load_Simulation, index_Simulation, zoneData_Simulation, load_DesignDay, index_DesignDay, zoneData_DesignDay);
                case LoadType.Heating:
                    return SpaceSimulationResult_Heating(load_Simulation, index_Simulation, zoneData_Simulation, load_DesignDay, index_DesignDay, zoneData_DesignDay);
            }

            return null;
        }

        private static SpaceSimulationResult SpaceSimulationResult_Cooling(float load_Simulation, int index_Simulation, ZoneData zoneData_Simulation, float load_DesignDay, int index_DesignDay, ZoneData zoneData_DesignDay)
        {
            if (float.IsNaN(load_Simulation) || float.IsNaN(load_DesignDay))
                return null;

            if (load_Simulation == 0 && load_DesignDay == 0)
                return SpaceSimulationResult(zoneData_Simulation, LoadType.Cooling);

            int index = -1;
            ZoneData zoneData = null;
            SizingMethod sizingMethod = SizingMethod.Undefined;

            if (load_Simulation > load_DesignDay)
            {
                sizingMethod = SizingMethod.Simulation;
                index = index_Simulation;
                zoneData = zoneData_Simulation;
            }
            else
            {
                sizingMethod = SizingMethod.CDD;
                index = index_DesignDay;
                zoneData = zoneData_DesignDay;
            }

            return SpaceSimulationResult(zoneData, index, LoadType.Cooling, sizingMethod);
        }

        private static SpaceSimulationResult SpaceSimulationResult_Heating(float load_Simulation, int index_Simulation, ZoneData zoneData_Simulation, float load_DesignDay, int index_DesignDay, ZoneData zoneData_DesignDay)
        {
            if (float.IsNaN(load_Simulation) || float.IsNaN(load_DesignDay))
                return null;

            if (load_Simulation == 0 && load_DesignDay == 0)
            {
                SpaceSimulationResult result = SpaceSimulationResult(zoneData_Simulation, LoadType.Heating);
                if (zoneData_DesignDay != null)
                {
                    double dryBulbTemp = zoneData_DesignDay.GetHourlyZoneResult(1, (short)tsdZoneArray.dryBulbTemp);
                    result.SetValue(SpaceSimulationResultParameter.DryBulbTempearture, dryBulbTemp);

                    double resultantTemp = zoneData_DesignDay.GetHourlyZoneResult(1, (short)tsdZoneArray.resultantTemp);
                    result.SetValue(SpaceSimulationResultParameter.ResultantTemperature, resultantTemp);
                }
                return result;
            }

            int index = -1;
            ZoneData zoneData = null;
            SizingMethod sizingMethod = SizingMethod.Undefined;

            if (load_Simulation > load_DesignDay)
            {
                sizingMethod = SizingMethod.Simulation;
                index = index_Simulation;
                zoneData = zoneData_Simulation;
            }
            else
            {
                sizingMethod = SizingMethod.HDD;
                index = index_DesignDay;
                zoneData = zoneData_DesignDay;
            }

            return Create.SpaceSimulationResult(zoneData, index, LoadType.Heating, sizingMethod);
        }
    }
}