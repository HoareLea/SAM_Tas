using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static WindTurbine ToTPD(this DisplaySystemWindTurbine displaySystemWindTurbine, PlantRoom plantRoom)
        {
            if (displaySystemWindTurbine == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddWindTurbine();
            result.Name = displaySystemWindTurbine.Name;
            result.Description = displaySystemWindTurbine.Description;

            displaySystemWindTurbine.SetLocation(result as PlantComponent);

            return result as WindTurbine;
        }
    }
}
