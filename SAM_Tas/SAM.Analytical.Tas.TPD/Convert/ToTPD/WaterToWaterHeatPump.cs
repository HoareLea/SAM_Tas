using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WaterToWaterHeatPump ToTPD(this DisplaySystemWaterToWaterHeatPump displaySystemWaterToWaterHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemWaterToWaterHeatPump == null || plantRoom == null)
            {
                return null;
            }

            WaterToWaterHeatPump result = plantRoom.AddWaterToWaterHeatPump();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterToWaterHeatPump.Name;
            @dynamic.Description = displaySystemWaterToWaterHeatPump.Description;

            result.HeatingSetpoint?.Update(displaySystemWaterToWaterHeatPump.HeatingSetpoint);
            result.CoolingSetpoint?.Update(displaySystemWaterToWaterHeatPump.CoolingSetpoint);
            result.HeatingEfficiency?.Update(displaySystemWaterToWaterHeatPump.HeatingEfficiency);
            result.CoolingEfficiency?.Update(displaySystemWaterToWaterHeatPump?.CoolingEfficiency);
            result.HeatingDuty?.Update(displaySystemWaterToWaterHeatPump.HeatingDuty, plantRoom);
            result.CoolingDuty?.Update(displaySystemWaterToWaterHeatPump.CoolingDuty, plantRoom);
            result.Capacity1 = displaySystemWaterToWaterHeatPump.Capacity1;
            result.DesignPressureDrop1 = displaySystemWaterToWaterHeatPump.DesignPressureDrop1;
            result.Capacity2 = displaySystemWaterToWaterHeatPump.Capacity2;
            result.DesignPressureDrop2 = displaySystemWaterToWaterHeatPump.DesignPressureDrop2;
            result.DesignDeltaT1 = displaySystemWaterToWaterHeatPump.DesignTemperatureDifference1;
            result.DesignDeltaT2 = displaySystemWaterToWaterHeatPump.DesignTemperatureDifference2;
            result.MotorEfficiency?.Update(displaySystemWaterToWaterHeatPump.MotorEfficiency);
            result.AncillaryLoad?.Update(displaySystemWaterToWaterHeatPump.AncillaryLoad);
            result.LossesInSizing = displaySystemWaterToWaterHeatPump.LossesInSizing.ToTPD();

            if (displaySystemWaterToWaterHeatPump.LossesInSizing || displaySystemWaterToWaterHeatPump.IsDomesticHotWater)
            {
                tpdWaterToWaterHeatPumpFlags tpdWaterToWaterHeatPumpFlags = displaySystemWaterToWaterHeatPump.LossesInSizing && displaySystemWaterToWaterHeatPump.IsDomesticHotWater ? tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpLossesInSizing | tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpIsDHW :
                    displaySystemWaterToWaterHeatPump.LossesInSizing ? tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpLossesInSizing : tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpIsDHW;

                result.Flags = (int)tpdWaterToWaterHeatPumpFlags;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterToWaterHeatPump.ScheduleName);

            displaySystemWaterToWaterHeatPump.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
