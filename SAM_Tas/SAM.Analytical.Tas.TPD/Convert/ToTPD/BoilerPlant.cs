using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static BoilerPlant ToTPD(this DisplaySystemBoiler displaySystemBoiler, PlantRoom plantRoom, BoilerPlant boilerPlant = null)
        {
            if (displaySystemBoiler == null || plantRoom == null)
            {
                return null;
            }

            BoilerPlant result = boilerPlant;
            if(result == null)
            {
                result = plantRoom.AddBoiler();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemBoiler.Name;
            @dynamic.Description = displaySystemBoiler.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemBoiler.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemBoiler.Efficiency, energyCentre);
            result.Duty?.Update(displaySystemBoiler.Duty, plantRoom);
            result.DesignDeltaT = displaySystemBoiler.DesignTemperatureDifference;
            result.Capacity = displaySystemBoiler.Capacity;
            result.DesignPressureDrop = displaySystemBoiler.DesignPressureDrop;
            result.AncillaryLoad?.Update(displaySystemBoiler.AncillaryLoad, energyCentre);
            result.LossesInSizing = displaySystemBoiler.LossesInSizing.ToTPD();

            if (displaySystemBoiler.LossesInSizing || displaySystemBoiler.IsDomesticHotWater)
            {
                tpdBoilerPlantFlags tpdBoilerPlantFlags = displaySystemBoiler.LossesInSizing && displaySystemBoiler.IsDomesticHotWater ? tpdBoilerPlantFlags.tpdBoilerPlantLossesInSizing | tpdBoilerPlantFlags.tpdBoilerPlantIsDHW:
                    displaySystemBoiler.LossesInSizing ? tpdBoilerPlantFlags.tpdBoilerPlantLossesInSizing : tpdBoilerPlantFlags.tpdBoilerPlantIsDHW;

                result.Flags = (int)tpdBoilerPlantFlags;
            }

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemBoiler.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemBoiler.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            Modify.SetSchedule((PlantComponent)result, displaySystemBoiler.ScheduleName);

            if(boilerPlant == null)
            {
                displaySystemBoiler.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
