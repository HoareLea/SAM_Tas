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

            Modify.SetSchedule((SystemComponent)result, displaySystemAirSourceChiller.ScheduleName);

            displaySystemAirSourceChiller.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
