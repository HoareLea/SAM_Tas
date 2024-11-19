using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Valve ToTPD(this DisplaySystemValve displaySystemValve, PlantRoom plantRoom)
        {
            if (displaySystemValve == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddValve();
            result.Name = displaySystemValve.Name;
            result.Description = displaySystemValve.Description;

            displaySystemValve.SetLocation(result as PlantComponent);

            return result as Valve;
        }
    }
}
