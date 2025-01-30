using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Valve ToTPD(this DisplaySystemValve displaySystemValve, PlantRoom plantRoom)
        {
            if (displaySystemValve == null || plantRoom == null)
            {
                return null;
            }

            Valve result = plantRoom.AddValve();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemValve.Name;
            @dynamic.Description = displaySystemValve.Description;

            result.Capacity = displaySystemValve.Capacity;
            result.DesignCapacitySignal = displaySystemValve.DesignCapacitySignal;
            result.DesignFlowRate = displaySystemValve.DesignFlowRate;
            result.DesignPressureDrop = displaySystemValve.DesignPressureDrop;

            Modify.SetSchedule((PlantComponent)result, displaySystemValve.ScheduleName);

            displaySystemValve.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
