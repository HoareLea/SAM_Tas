using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SurfaceWaterExchanger ToTPD(this DisplaySystemSurfaceWaterExchanger displaySystemSurfaceWaterExchanger, PlantRoom plantRoom)
        {
            if (displaySystemSurfaceWaterExchanger == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddSurfaceWaterExchanger();
            result.Name = displaySystemSurfaceWaterExchanger.Name;
            result.Description = displaySystemSurfaceWaterExchanger.Description;

            displaySystemSurfaceWaterExchanger.SetLocation(result as PlantComponent);

            return result as SurfaceWaterExchanger;
        }
    }
}
