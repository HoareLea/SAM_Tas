using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatPump ToTPD(this DisplaySystemWaterSourceHeatPump displaySystemWaterSourceHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }

            HeatPump result = plantRoom.AddHeatPump();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceHeatPump.Name;
            @dynamic.Description = displaySystemWaterSourceHeatPump.Description;

            result.Type = displaySystemWaterSourceHeatPump.HeatPumpType.ToTPD();
            result.CoolingCapacity?.Update(displaySystemWaterSourceHeatPump.CoolingCapacity, plantRoom);
            result.CoolingPower?.Update(displaySystemWaterSourceHeatPump.CoolingPower);
            result.HeatingCapacity?.Update(displaySystemWaterSourceHeatPump.HeatingCapacity);
            result.HeatingPower?.Update(displaySystemWaterSourceHeatPump.HeatingPower);
            result.HeatCoolDutyRatio = displaySystemWaterSourceHeatPump.HeatingCoolingDutyRatio;
            result.HeatCapPowRatio = displaySystemWaterSourceHeatPump.HeatingCapacityPowerRatio;
            result.CoolCapPowRatio = displaySystemWaterSourceHeatPump.CoolingCapacityPowerRatio;
            result.DesignPressureDrop = displaySystemWaterSourceHeatPump.DesignPressureDrop;
            result.Capacity = displaySystemWaterSourceHeatPump.Capacity;
            result.DesignDeltaT = displaySystemWaterSourceHeatPump.DesignTemperatureDifference;
            result.StandbyPower = displaySystemWaterSourceHeatPump.StandbyPower;
            result.ADFHeatMode = displaySystemWaterSourceHeatPump.ADFHeatingMode;
            result.ADFCoolMode = displaySystemWaterSourceHeatPump.ADFCoolingMode;
            result.PowHeatPort = displaySystemWaterSourceHeatPump.PortHeatingPower;
            result.PowCoolPort = displaySystemWaterSourceHeatPump.PortCoolingPower;
            result.MotorEfficiency?.Update(displaySystemWaterSourceHeatPump.MotorEfficiency);
            result.HeatSizeFraction = displaySystemWaterSourceHeatPump.HeatSizeFraction;
            result.AncillaryLoad?.Update(displaySystemWaterSourceHeatPump.AncillaryLoad);

            if (displaySystemWaterSourceHeatPump.IsDomesticHotWater)
            {
                result.Flags = (int)tpdHeatPumpFlags.tpdHeatPumpIsDHW;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterSourceHeatPump.ScheduleName);

            displaySystemWaterSourceHeatPump.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
