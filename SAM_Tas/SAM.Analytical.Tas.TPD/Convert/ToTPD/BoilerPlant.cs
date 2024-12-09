using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static BoilerPlant ToTPD(this DisplaySystemBoiler displaySystemAirSourceHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddBoiler();
            result.Name = displaySystemAirSourceHeatPump.Name;
            result.Description = displaySystemAirSourceHeatPump.Description;

            displaySystemAirSourceHeatPump.SetLocation(result as PlantComponent);

            return result as BoilerPlant;
        }
    }
}
