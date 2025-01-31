using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static BoilerPlant ToTPD(this DisplaySystemBoiler displaySystemBoiler, PlantRoom plantRoom)
        {
            if (displaySystemBoiler == null || plantRoom == null)
            {
                return null;
            }

            BoilerPlant result = plantRoom.AddBoiler();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemBoiler.Name;
            @dynamic.Description = displaySystemBoiler.Description;

            result.Setpoint?.Update(displaySystemBoiler.Setpoint);
            result.Efficiency?.Update(displaySystemBoiler.Efficiency);
            result.Duty?.Update(displaySystemBoiler.Duty, plantRoom);
            result.DesignDeltaT = displaySystemBoiler.DesignTemperatureDifference;
            result.Capacity = displaySystemBoiler.Capacity;
            result.DesignPressureDrop = displaySystemBoiler.DesignPressureDrop;
            result.AncillaryLoad?.Update(displaySystemBoiler.AncillaryLoad);
            result.LossesInSizing = displaySystemBoiler.LossesInSizing.ToTPD();

            if (displaySystemBoiler.LossesInSizing || displaySystemBoiler.IsDomesticHotWater)
            {
                tpdBoilerPlantFlags tpdBoilerPlantFlags = displaySystemBoiler.LossesInSizing && displaySystemBoiler.IsDomesticHotWater ? tpdBoilerPlantFlags.tpdBoilerPlantLossesInSizing | tpdBoilerPlantFlags.tpdBoilerPlantIsDHW:
                    displaySystemBoiler.LossesInSizing ? tpdBoilerPlantFlags.tpdBoilerPlantLossesInSizing : tpdBoilerPlantFlags.tpdBoilerPlantIsDHW;

                result.Flags = (int)tpdBoilerPlantFlags;
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemBoiler.ScheduleName);

            displaySystemBoiler.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
