using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DryCooler ToTPD(this DisplaySystemDryCooler displaySystemDryCooler, PlantRoom plantRoom)
        {
            if (displaySystemDryCooler == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddCoolingTower();
            result.Name = displaySystemDryCooler.Name;
            result.Description = displaySystemDryCooler.Description;

            displaySystemDryCooler.SetLocation(result as PlantComponent);

            return result as DryCooler;
        }
    }
}
