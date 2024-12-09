using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HorizontalGHE ToTPD(this DisplaySystemHorizontalExchanger displaySystemHorizontalExchanger, PlantRoom plantRoom)
        {
            if (displaySystemHorizontalExchanger == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddHorizontalGHE();
            result.Name = displaySystemHorizontalExchanger.Name;
            result.Description = displaySystemHorizontalExchanger.Description;

            displaySystemHorizontalExchanger.SetLocation(result as PlantComponent);

            return result as HorizontalGHE;
        }
    }
}
