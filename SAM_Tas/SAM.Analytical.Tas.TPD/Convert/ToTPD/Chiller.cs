using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Chiller ToTPD(this DisplaySystemAirSourceChiller displaySystemAirSourceChiller, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceChiller == null || plantRoom == null)
            {
                return null;
            }
            

            dynamic result = plantRoom.AddChiller();
            result.Name = displaySystemAirSourceChiller.Name;
            result.Description = displaySystemAirSourceChiller.Description;

            result.IsDirectAbsChiller = false;

            displaySystemAirSourceChiller.SetLocation(result as PlantComponent);

            return result as Chiller;
        }

        public static AbsorptionChiller ToTPD(this DisplaySystemAirSourceDirectAbsorptionChiller displaySystemAirSourceDirectAbsorptionChiller, PlantRoom plantRoom)
        {
            if (displaySystemAirSourceDirectAbsorptionChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddChiller();
            result.Name = displaySystemAirSourceDirectAbsorptionChiller.Name;
            result.Description = displaySystemAirSourceDirectAbsorptionChiller.Description;

            result.IsDirectAbsChiller = true;


            displaySystemAirSourceDirectAbsorptionChiller.SetLocation(result as PlantComponent);

            return result as AbsorptionChiller;
        }
    }
}
