using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DHWGroup ToTPD(this DisplayDomesticHotWaterSystemCollection displayDomesticHotWaterSystemCollection, PlantRoom plantRoom)
        {
            if (displayDomesticHotWaterSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddDHWGroup();
            result.Name = displayDomesticHotWaterSystemCollection.Name;
            result.Description = displayDomesticHotWaterSystemCollection.Description;

            displayDomesticHotWaterSystemCollection.SetLocation(result as PlantComponent);

            return result as DHWGroup;
        }
    }
}
