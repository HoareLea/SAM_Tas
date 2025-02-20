using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AirSourceHeatPump ToTPD(this DisplaySystemAirSourceHeatPump displaySystemAirSourceHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }

            AirSourceHeatPump result = plantRoom.AddAirSourceHeatPump();
            
            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirSourceHeatPump.Name;
            @dynamic.Description = displaySystemAirSourceHeatPump.Description;

            result.Type = displaySystemAirSourceHeatPump.HeatPumpType.ToTPD();
            result.CoolingCapacity?.Update(displaySystemAirSourceHeatPump.CoolingCapacity, plantRoom);
            result.CoolingPower?.Update(displaySystemAirSourceHeatPump.CoolingPower);
            result.HeatingCapacity?.Update(displaySystemAirSourceHeatPump.HeatingCapacity);
            result.HeatingPower?.Update(displaySystemAirSourceHeatPump.HeatingPower);
            result.CondenserFanLoad?.Update(displaySystemAirSourceHeatPump.CondenserFanLoad);
            result.HeatCoolDutyRatio = displaySystemAirSourceHeatPump.HeatingCoolingDutyRatio;
            result.HeatCapPowRatio = displaySystemAirSourceHeatPump.HeatingCapacityPowerRatio;
            result.CoolCapPowRatio = displaySystemAirSourceHeatPump.CoolingCapacityPowerRatio;
            result.MaxDemFanRatio = displaySystemAirSourceHeatPump.MaxDemandFanRatio;
            result.StandbyPower = displaySystemAirSourceHeatPump.StandbyPower;
            result.ADFHeatMode = displaySystemAirSourceHeatPump.ADFHeatingMode;
            result.ADFCoolMode = displaySystemAirSourceHeatPump.ADFCoolingMode;
            result.PowHeatPort = displaySystemAirSourceHeatPump.PortHeatingPower;
            result.PowCoolPort = displaySystemAirSourceHeatPump.PortCoolingPower;
            result.WaterPipeLength = displaySystemAirSourceHeatPump.WaterPipeLength;
            result.AncillaryLoad?.Update(displaySystemAirSourceHeatPump.AncillaryLoad);
            result.HeatSizeFraction = displaySystemAirSourceHeatPump.HeatSizeFraction;

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.FanEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceHeatPump.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(3, fuelSource);
            }

            if (displaySystemAirSourceHeatPump.IsDomesticHotWater)
            {
                result.Flags = (int)tpdAirSourceHeatPumpFlags.tpdAirSourceHeatPumpIsDHW;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemAirSourceHeatPump.ScheduleName);

            displaySystemAirSourceHeatPump.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
