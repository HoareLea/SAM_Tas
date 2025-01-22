using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static BoilerPlant ToTPD(this DisplaySystemBoiler displaySystemAirSourceHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }

            BoilerPlant result = plantRoom.AddBoiler();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirSourceHeatPump.Name;
            @dynamic.Description = displaySystemAirSourceHeatPump.Description;

            result.Setpoint?.Update(displaySystemAirSourceHeatPump.Setpoint);
            result.Efficiency?.Update(displaySystemAirSourceHeatPump.Efficiency);
            result.Duty?.Update(displaySystemAirSourceHeatPump.Duty);
            result.DesignDeltaT = displaySystemAirSourceHeatPump.DesignTemperatureDifference;
            result.Capacity = displaySystemAirSourceHeatPump.Capacity;
            result.DesignPressureDrop = displaySystemAirSourceHeatPump.DesignPressureDrop;
            result.AncillaryLoad?.Update(displaySystemAirSourceHeatPump.AncillaryLoad);
            result.LossesInSizing = displaySystemAirSourceHeatPump.LossesInSizing.ToTPD();

            if (displaySystemAirSourceHeatPump.LossesInSizing || displaySystemAirSourceHeatPump.IsDomesticHotWater)
            {
                tpdBoilerPlantFlags tpdBoilerPlantFlags = displaySystemAirSourceHeatPump.LossesInSizing && displaySystemAirSourceHeatPump.IsDomesticHotWater ? tpdBoilerPlantFlags.tpdBoilerPlantLossesInSizing | tpdBoilerPlantFlags.tpdBoilerPlantIsDHW:
                    displaySystemAirSourceHeatPump.LossesInSizing ? tpdBoilerPlantFlags.tpdBoilerPlantLossesInSizing : tpdBoilerPlantFlags.tpdBoilerPlantIsDHW;

                result.Flags = (int)tpdBoilerPlantFlags;
            }

            displaySystemAirSourceHeatPump.SetLocation(result as PlantComponent);

            return result as BoilerPlant;
        }
    }
}
