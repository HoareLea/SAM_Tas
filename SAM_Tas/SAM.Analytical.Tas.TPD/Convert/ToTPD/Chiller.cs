using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Chiller ToTPD(this DisplaySystemAirSourceChiller displaySystemAirSourceChiller, PlantRoom plantRoom, Chiller chiller = null)
        {
            if (displaySystemAirSourceChiller == null || plantRoom == null)
            {
                return null;
            }


            Chiller result = chiller;
            if(result == null)
            {
                result = plantRoom.AddChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirSourceChiller.Name;
            @dynamic.Description = displaySystemAirSourceChiller.Description;
            @dynamic.IsDirectAbsChiller = false;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemAirSourceChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemAirSourceChiller.Efficiency, energyCentre);
            result.CondenserFanLoad?.Update(displaySystemAirSourceChiller.CondenserFanLoad, energyCentre);
            result.Duty?.Update(displaySystemAirSourceChiller.Duty, plantRoom);
            result.DesignDeltaT = displaySystemAirSourceChiller.DesignTemperatureDifference;
            result.Capacity = displaySystemAirSourceChiller.Capacity;
            result.DesignPressureDrop = displaySystemAirSourceChiller.DesignPressureDrop;
            result.LossesInSizing = displaySystemAirSourceChiller.LossesInSizing.ToTPD();

            Modify.SetSchedule((PlantComponent)result, displaySystemAirSourceChiller.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceChiller.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceChiller.GetValue<string>(Core.Systems.SystemObjectParameter.FanEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(chiller == null)
            {
                displaySystemAirSourceChiller.SetLocation(result as PlantComponent);
            }

            return result;
        }

        public static Chiller ToTPD(this DisplaySystemAirSourceDirectAbsorptionChiller displaySystemAirSourceDirectAbsorptionChiller, PlantRoom plantRoom, Chiller chiller = null)
        {
            if (displaySystemAirSourceDirectAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            Chiller result = chiller;
            if (result == null)
            {
                result = plantRoom.AddChiller();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirSourceDirectAbsorptionChiller.Name;
            @dynamic.Description = displaySystemAirSourceDirectAbsorptionChiller.Description;
            @dynamic.IsDirectAbsChiller = true;

            //result.Setpoint?.Update(displaySystemAirSourceDirectAbsorptionChiller.Setpoint);
            //result.Efficiency?.Update(displaySystemAirSourceDirectAbsorptionChiller.Efficiency);
            //result.Duty?.Update(displaySystemAirSourceDirectAbsorptionChiller.Duty, plantRoom);
            //result.Capacity = displaySystemAirSourceDirectAbsorptionChiller.Capacity;
            //result.DesignPressureDrop = displaySystemAirSourceDirectAbsorptionChiller.DesignPressureDrop;
            ////result.AncillaryLoad?.Update(displaySystemAirSourceDirectAbsorptionChiller.AnciliaryLoad);
            ////result.MinOutTempSource?.Update(displaySystemAirSourceDirectAbsorptionChiller.MinOutTempSource);
            //result.LossesInSizing = displaySystemAirSourceDirectAbsorptionChiller.LossesInSizing.ToTPD();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Setpoint?.Update(displaySystemAirSourceDirectAbsorptionChiller.Setpoint, energyCentre);
            result.Efficiency?.Update(displaySystemAirSourceDirectAbsorptionChiller.Efficiency, energyCentre);
            result.CondenserFanLoad?.Update(displaySystemAirSourceDirectAbsorptionChiller.CondenserFanLoad, energyCentre);
            result.Duty?.Update(displaySystemAirSourceDirectAbsorptionChiller.Duty, plantRoom);
            result.DesignDeltaT = displaySystemAirSourceDirectAbsorptionChiller.DesignTemperatureDifference;
            result.Capacity = displaySystemAirSourceDirectAbsorptionChiller.Capacity;
            result.DesignPressureDrop = displaySystemAirSourceDirectAbsorptionChiller.DesignPressureDrop;
            result.LossesInSizing = displaySystemAirSourceDirectAbsorptionChiller.LossesInSizing.ToTPD();


            Modify.SetSchedule((PlantComponent)result, displaySystemAirSourceDirectAbsorptionChiller.ScheduleName);

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceDirectAbsorptionChiller.GetValue<string>(Core.Systems.SystemObjectParameter.EnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemAirSourceDirectAbsorptionChiller.GetValue<string>(Core.Systems.SystemObjectParameter.FanEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(chiller == null)
            {
                displaySystemAirSourceDirectAbsorptionChiller.SetLocation(result as PlantComponent);
            }


            return result;
        }
    }
}
