using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static MultiBoiler ToTPD(this DisplaySystemMultiBoiler displaySystemMultiBoiler, PlantRoom plantRoom)
        {
            if (displaySystemMultiBoiler == null || plantRoom == null)
            {
                return null;
            }

            MultiBoiler result = plantRoom.AddMultiBoiler();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemMultiBoiler.Name;
            @dynamic.Description = displaySystemMultiBoiler.Description;

            result.Setpoint = displaySystemMultiBoiler.Setpoint.ToTPD();
            result.Duty = displaySystemMultiBoiler.Duty.ToTPD();
            result.DesignDeltaT = displaySystemMultiBoiler.DesignTemperatureDifference;
            result.Capacity = displaySystemMultiBoiler.Capacity;
            result.DesignPressureDrop = displaySystemMultiBoiler.DesignPressureDrop;
            result.Sequence = displaySystemMultiBoiler.Sequence.ToTPD();
            result.Multiplicity = displaySystemMultiBoiler.Multiplicity;
            result.LossesInSizing = displaySystemMultiBoiler.LossesInSizing ? 1 : 0;

            displaySystemMultiBoiler.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
