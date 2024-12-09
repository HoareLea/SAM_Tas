using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WaterToWaterHeatPump ToTPD(this DisplaySystemWaterToWaterHeatPump displaySystemWaterToWaterHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemWaterToWaterHeatPump == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddWaterToWaterHeatPump();
            result.Name = displaySystemWaterToWaterHeatPump.Name;
            result.Description = displaySystemWaterToWaterHeatPump.Description;

            displaySystemWaterToWaterHeatPump.SetLocation(result as PlantComponent);

            return result as WaterToWaterHeatPump;
        }
    }
}
