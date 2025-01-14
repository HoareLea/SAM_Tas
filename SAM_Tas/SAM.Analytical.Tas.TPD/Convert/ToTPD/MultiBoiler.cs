using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static MultiBoiler ToTPD(this DisplaySystemMultiBoiler displaySystemMultiBoiler, PlantRoom plantRoom)
        {
            if (displaySystemMultiBoiler == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddMultiBoiler();
            result.Name = displaySystemMultiBoiler.Name;
            result.Description = displaySystemMultiBoiler.Description;

            displaySystemMultiBoiler.SetLocation(result as PlantComponent);

            return result as MultiBoiler;
        }
    }
}
