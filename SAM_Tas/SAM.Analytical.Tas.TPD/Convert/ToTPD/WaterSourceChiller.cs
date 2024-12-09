using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WaterSourceChiller ToTPD(this DisplaySystemWaterSourceChiller displaySystemWaterSourceChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddWaterSourceChiller();
            result.Name = displaySystemWaterSourceChiller.Name;
            result.Description = displaySystemWaterSourceChiller.Description;

            result.IsDirectAbsChiller = 1;

            displaySystemWaterSourceChiller.SetLocation(result as PlantComponent);

            return result as WaterSourceChiller;
        }

        public static WaterSourceChiller ToTPD(this DisplaySystemWaterSourceDirectAbsorptionChiller displaySystemWaterSourceDirectAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceDirectAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddWaterSourceChiller();
            result.Name = displaySystemWaterSourceDirectAbsorptionChiller.Name;
            result.Description = displaySystemWaterSourceDirectAbsorptionChiller.Description;

            result.IsDirectAbsChiller = -1;

            displaySystemWaterSourceDirectAbsorptionChiller.SetLocation(result as PlantComponent);

            return result as WaterSourceChiller;
        }
    }
}
