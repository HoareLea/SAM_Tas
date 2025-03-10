using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatPump ToTPD(this DisplaySystemWaterSourceHeatPump displaySystemWaterSourceHeatPump, PlantRoom plantRoom, HeatPump heatPump = null)
        {
            if (displaySystemWaterSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }

            HeatPump result = heatPump;
            if(result == null)
            {
                result = plantRoom.AddHeatPump();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceHeatPump.Name;
            @dynamic.Description = displaySystemWaterSourceHeatPump.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Type = displaySystemWaterSourceHeatPump.HeatPumpType.ToTPD();
            result.CoolingCapacity?.Update(displaySystemWaterSourceHeatPump.CoolingCapacity, plantRoom);
            result.CoolingPower?.Update(displaySystemWaterSourceHeatPump.CoolingPower, energyCentre);
            result.HeatingCapacity?.Update(displaySystemWaterSourceHeatPump.HeatingCapacity, energyCentre);
            result.HeatingPower?.Update(displaySystemWaterSourceHeatPump.HeatingPower, energyCentre);
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
            result.MotorEfficiency?.Update(displaySystemWaterSourceHeatPump.MotorEfficiency, energyCentre);
            result.HeatSizeFraction = displaySystemWaterSourceHeatPump.HeatSizeFraction;
            result.AncillaryLoad?.Update(displaySystemWaterSourceHeatPump.AncillaryLoad, energyCentre);

            if (displaySystemWaterSourceHeatPump.IsDomesticHotWater)
            {
                result.Flags = (int)tpdHeatPumpFlags.tpdHeatPumpIsDHW;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemWaterSourceHeatPump.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemWaterSourceHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(heatPump == null)
            {
                displaySystemWaterSourceHeatPump.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
