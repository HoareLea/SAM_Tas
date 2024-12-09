using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CHP ToTPD(this DisplaySystemCHP displaySystemCHP, PlantRoom plantRoom)
        {
            if (displaySystemCHP == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddChp();
            result.Name = displaySystemCHP.Name;
            result.Description = displaySystemCHP.Description;

            displaySystemCHP.SetLocation(result as PlantComponent);

            return result as CHP;
        }
    }
}
