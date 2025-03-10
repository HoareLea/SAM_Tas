using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AbsorptionChiller ToTPD(this DisplaySystemAbsorptionChiller displaySystemAbsorptionChiller, PlantRoom plantRoom, AbsorptionChiller absorptionChiller = null)
        {
            if (displaySystemAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            AbsorptionChiller result = absorptionChiller;
            if(result == null)
            {
                result = plantRoom.AddAbsorptionChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAbsorptionChiller.Name;
            @dynamic.Description = displaySystemAbsorptionChiller.Description;
            @dynamic.IsWaterSource = false;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemAbsorptionChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemAbsorptionChiller.Efficiency, energyCentre);
            result.Duty?.Update(displaySystemAbsorptionChiller.Duty, plantRoom);
            result.Capacity1 = displaySystemAbsorptionChiller.Capacity1;
            result.DesignPressureDrop1 = displaySystemAbsorptionChiller.DesignPressureDrop1;
            result.Capacity2 = displaySystemAbsorptionChiller.Capacity2;
            result.DesignPressureDrop2 = displaySystemAbsorptionChiller.DesignPressureDrop2;
            result.AncillaryLoad?.Update(displaySystemAbsorptionChiller.AncillaryLoad, energyCentre);
            result.MinOutTempSource?.Update(displaySystemAbsorptionChiller.MinimalOutSourceTemperature, energyCentre);

            FuelSource fuelSource = plantRoom.FuelSource(displaySystemAbsorptionChiller.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }
            
            if(absorptionChiller == null)
            {
                displaySystemAbsorptionChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }

        public static AbsorptionChiller ToTPD(this DisplaySystemWaterSourceAbsorptionChiller displaySystemWaterSourceAbsorptionChiller, PlantRoom plantRoom, AbsorptionChiller absorptionChiller = null)
        {
            if (displaySystemWaterSourceAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            AbsorptionChiller result = absorptionChiller;
            if (result == null)
            {
                result = plantRoom.AddAbsorptionChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemWaterSourceAbsorptionChiller.Name;
            @dynamic.Description = displaySystemWaterSourceAbsorptionChiller.Description;
            @dynamic.IsWaterSource = true;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemWaterSourceAbsorptionChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemWaterSourceAbsorptionChiller.Efficiency, energyCentre);
            result.Duty?.Update(displaySystemWaterSourceAbsorptionChiller.Duty, plantRoom);
            result.Capacity1 = displaySystemWaterSourceAbsorptionChiller.Capacity1;
            result.Capacity2 = displaySystemWaterSourceAbsorptionChiller.Capacity2;
            result.DesignPressureDrop1 = displaySystemWaterSourceAbsorptionChiller.DesignPressureDrop1;
            result.DesignPressureDrop2 = displaySystemWaterSourceAbsorptionChiller.DesignPressureDrop2;
            result.DesignPressureDrop3 = displaySystemWaterSourceAbsorptionChiller.DesignPressureDrop3;
            result.AncillaryLoad?.Update(displaySystemWaterSourceAbsorptionChiller.AncillaryLoad, energyCentre);
            result.MinOutTempSource?.Update(displaySystemWaterSourceAbsorptionChiller.MinimalOutSourceTemperature, energyCentre);

            FuelSource fuelSource = plantRoom.FuelSource(displaySystemWaterSourceAbsorptionChiller.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            if (absorptionChiller == null)
            {
                displaySystemWaterSourceAbsorptionChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }

    }
}
