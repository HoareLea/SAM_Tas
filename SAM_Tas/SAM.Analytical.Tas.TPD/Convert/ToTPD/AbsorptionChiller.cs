using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AbsorptionChiller ToTPD(this DisplaySystemAbsorptionChiller displaySystemAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddAbsorptionChiller();
            result.Name = displaySystemAbsorptionChiller.Name;
            result.Description = displaySystemAbsorptionChiller.Description;

            result.IsWaterSource = false;

            displaySystemAbsorptionChiller.SetLocation(result as PlantComponent);

            return result as AbsorptionChiller;
        }

        public static AbsorptionChiller ToTPD(this DisplaySystemWaterSourceAbsorptionChiller displaySystemWaterSourceAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemWaterSourceAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddAbsorptionChiller();
            result.Name = displaySystemWaterSourceAbsorptionChiller.Name;
            result.Description = displaySystemWaterSourceAbsorptionChiller.Description;

            result.IsWaterSource = true;

            displaySystemWaterSourceAbsorptionChiller.SetLocation(result as PlantComponent);

            return result as AbsorptionChiller;
        }
    }
}
