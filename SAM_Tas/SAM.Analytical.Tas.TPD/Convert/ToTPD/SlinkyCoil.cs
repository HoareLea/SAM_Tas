using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SlinkyCoil ToTPD(this DisplaySystemSlinkyCoil displaySystemSlinkyCoil, PlantRoom plantRoom)
        {
            if (displaySystemSlinkyCoil == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddSlinkyCoil();
            result.Name = displaySystemSlinkyCoil.Name;
            result.Description = displaySystemSlinkyCoil.Description;

            displaySystemSlinkyCoil.SetLocation(result as PlantComponent);

            return result as SlinkyCoil;
        }
    }
}
