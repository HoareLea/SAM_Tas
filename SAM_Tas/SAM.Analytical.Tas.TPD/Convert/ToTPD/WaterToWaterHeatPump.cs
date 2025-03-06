using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WaterToWaterHeatPump ToTPD(this DisplaySystemWaterToWaterHeatPump displaySystemWaterToWaterHeatPump, PlantRoom plantRoom, WaterToWaterHeatPump waterToWaterHeatPump = null)
        {
            if (displaySystemWaterToWaterHeatPump == null || plantRoom == null)
            {
                return null;
            }

            WaterToWaterHeatPump result = waterToWaterHeatPump;
            if(result == null)
            {
                result = plantRoom.AddWaterToWaterHeatPump();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterToWaterHeatPump.Name;
            @dynamic.Description = displaySystemWaterToWaterHeatPump.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.HeatingSetpoint?.Update(displaySystemWaterToWaterHeatPump.HeatingSetpoint, energyCentre);
            result.CoolingSetpoint?.Update(displaySystemWaterToWaterHeatPump.CoolingSetpoint, energyCentre);
            result.HeatingEfficiency?.Update(displaySystemWaterToWaterHeatPump.HeatingEfficiency, energyCentre);
            result.CoolingEfficiency?.Update(displaySystemWaterToWaterHeatPump?.CoolingEfficiency, energyCentre);
            result.HeatingDuty?.Update(displaySystemWaterToWaterHeatPump.HeatingDuty, plantRoom);
            result.CoolingDuty?.Update(displaySystemWaterToWaterHeatPump.CoolingDuty, plantRoom);
            result.Capacity1 = displaySystemWaterToWaterHeatPump.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterToWaterHeatPump.DesignPressureDrop1;
            result.Capacity2 = displaySystemWaterToWaterHeatPump.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterToWaterHeatPump.DesignPressureDrop2;
            result.DesignDeltaT1 = displaySystemWaterToWaterHeatPump.DesignTemperatureDifference1;
            result.DesignDeltaT2 = displaySystemWaterToWaterHeatPump.DesignTemperatureDifference2;
            result.MotorEfficiency?.Update(displaySystemWaterToWaterHeatPump.MotorEfficiency, energyCentre);
            result.AncillaryLoad?.Update(displaySystemWaterToWaterHeatPump.AncillaryLoad, energyCentre);
            result.LossesInSizing = displaySystemWaterToWaterHeatPump.LossesInSizing.ToTPD();

            if (displaySystemWaterToWaterHeatPump.LossesInSizing || displaySystemWaterToWaterHeatPump.IsDomesticHotWater)
            {
                tpdWaterToWaterHeatPumpFlags tpdWaterToWaterHeatPumpFlags = displaySystemWaterToWaterHeatPump.LossesInSizing && displaySystemWaterToWaterHeatPump.IsDomesticHotWater ? tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpLossesInSizing | tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpIsDHW :
                    displaySystemWaterToWaterHeatPump.LossesInSizing ? tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpLossesInSizing : tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpIsDHW;

                result.Flags = (int)tpdWaterToWaterHeatPumpFlags;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterToWaterHeatPump.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemWaterToWaterHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemWaterToWaterHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(waterToWaterHeatPump == null)
            {
                displaySystemWaterToWaterHeatPump.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
