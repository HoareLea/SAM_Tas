using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static AirSourceHeatPump ToTPD(this DisplaySystemAirSourceHeatPump displaySystemAirSourceHeatPump, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceHeatPump == null || plantRoom == null)
            {
                return null;
            }


            dynamic result = plantRoom.AddAirSourceHeatPump();
            result.Name = displaySystemAirSourceHeatPump.Name;
            result.Description = displaySystemAirSourceHeatPump.Description;

            displaySystemAirSourceHeatPump.SetLocation(result as PlantComponent);

            return result as AirSourceHeatPump;
        }
    }
}
