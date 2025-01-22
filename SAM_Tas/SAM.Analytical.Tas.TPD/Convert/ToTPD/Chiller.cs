using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Chiller ToTPD(this DisplaySystemAirSourceChiller displaySystemAirSourceChiller, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceChiller == null || plantRoom == null)
            {
                return null;
            }
            

            Chiller result = plantRoom.AddChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirSourceChiller.Name;
            @dynamic.Description = displaySystemAirSourceChiller.Description;
            @dynamic.IsDirectAbsChiller = false;

            result.Setpoint?.Update(displaySystemAirSourceChiller.Setpoint);
            result.Efficiency?.Update(displaySystemAirSourceChiller.Efficiency);
            result.CondenserFanLoad?.Update(displaySystemAirSourceChiller.CondenserFanLoad);
            result.Duty?.Update(displaySystemAirSourceChiller.Duty);
            result.DesignDeltaT = displaySystemAirSourceChiller.DesignTemperatureDifference;
            result.Capacity = displaySystemAirSourceChiller.Capacity;
            result.DesignPressureDrop = displaySystemAirSourceChiller.DesignPressureDrop;
            result.LossesInSizing = displaySystemAirSourceChiller.LossesInSizing.ToTPD();

            displaySystemAirSourceChiller.SetLocation(result as PlantComponent);

            return result;
        }

        public static AbsorptionChiller ToTPD(this DisplaySystemAirSourceDirectAbsorptionChiller displaySystemAirSourceDirectAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceDirectAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            AbsorptionChiller result = plantRoom.AddAbsorptionChiller();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemAirSourceDirectAbsorptionChiller.Name;
            @dynamic.Description = displaySystemAirSourceDirectAbsorptionChiller.Description;
            @dynamic.IsDirectAbsChiller = true;

            result.Setpoint?.Update(displaySystemAirSourceDirectAbsorptionChiller.Setpoint);
            result.Efficiency?.Update(displaySystemAirSourceDirectAbsorptionChiller.Efficiency);
            result.Duty?.Update(displaySystemAirSourceDirectAbsorptionChiller.Duty);
            result.Capacity1 = displaySystemAirSourceDirectAbsorptionChiller.Capacity;
            result.DesignPressureDrop1 = displaySystemAirSourceDirectAbsorptionChiller.DesignPressureDrop;
            //result.AncillaryLoad?.Update(displaySystemAirSourceDirectAbsorptionChiller.AnciliaryLoad);
            //result.MinOutTempSource?.Update(displaySystemAirSourceDirectAbsorptionChiller.MinOutTempSource);
            result.LossesInSizing = displaySystemAirSourceDirectAbsorptionChiller.LossesInSizing.ToTPD();

            displaySystemAirSourceDirectAbsorptionChiller.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
