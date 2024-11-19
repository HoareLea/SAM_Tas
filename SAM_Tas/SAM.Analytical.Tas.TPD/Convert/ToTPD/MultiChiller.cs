using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static MultiChiller ToTPD(this DisplaySystemMultiChiller displaySystemMultiChiller, PlantRoom plantRoom)
        {
            if (displaySystemMultiChiller == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddMultiChiller();
            result.Name = displaySystemMultiChiller.Name;
            result.Description = displaySystemMultiChiller.Description;

            displaySystemMultiChiller.SetLocation(result as PlantComponent);

            return result as MultiChiller;
        }
    }
}
