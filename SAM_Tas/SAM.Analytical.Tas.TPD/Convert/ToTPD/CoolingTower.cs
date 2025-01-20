using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingTower ToTPD(this DisplaySystemCoolingTower displaySystemCoolingTower, PlantRoom plantRoom)
        {
            if (displaySystemCoolingTower == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddCoolingTower();
            result.Name = displaySystemCoolingTower.Name;
            result.Description = displaySystemCoolingTower.Description;

            displaySystemCoolingTower.SetLocation(result as PlantComponent);

            return result as CoolingTower;
        }
    }
}
