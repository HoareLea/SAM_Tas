using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Pump ToTPD(this DisplaySystemPump displaySystemPump, PlantRoom plantRoom)
        {
            if (displaySystemPump == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddPump();
            result.Name = displaySystemPump.Name;
            result.Description = displaySystemPump.Description;

            displaySystemPump.SetLocation(result as PlantComponent);

            return result as Pump;
        }
    }
}
