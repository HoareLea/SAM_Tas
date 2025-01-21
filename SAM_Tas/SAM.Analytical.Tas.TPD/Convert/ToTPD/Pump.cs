using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Pump ToTPD(this DisplaySystemPump displaySystemPump, PlantRoom plantRoom)
        {
            if (displaySystemPump == null || plantRoom == null)
            {
                return null;
            }

            Pump result = plantRoom.AddPump();
            
            dynamic @dynamic = result;
            @dynamic.Name = displaySystemPump.Name;
            @dynamic.Description = displaySystemPump.Description;

            result.OverallEfficiency = displaySystemPump.OverallEfficiency.ToTPD();
            result.Pressure = displaySystemPump.Pressure;
            result.DesignFlowRate = displaySystemPump.DesignFlowRate;
            result.Capacity = displaySystemPump.Capacity;
            result.PartLoad = displaySystemPump.PartLoad.ToTPD();

            displaySystemPump.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
