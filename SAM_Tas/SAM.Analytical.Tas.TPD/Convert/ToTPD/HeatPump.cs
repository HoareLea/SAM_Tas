using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatPump ToTPD(this DisplaySystemWaterSourceHeatPump displaySystemWaterSourceHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddHeatPump();
            result.Name = displaySystemWaterSourceHeatPump.Name;
            result.Description = displaySystemWaterSourceHeatPump.Description;

            displaySystemWaterSourceHeatPump.SetLocation(result as PlantComponent);

            return result as HeatPump;
        }
    }
}
