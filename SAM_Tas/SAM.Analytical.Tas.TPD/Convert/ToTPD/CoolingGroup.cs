using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingGroup ToTPD(this DisplayCoolingSystemCollection displayCoolingSystemCollection, PlantRoom plantRoom)
        {
            if (displayCoolingSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddCoolingGroup();
            result.Name = displayCoolingSystemCollection.Name;
            result.Description = displayCoolingSystemCollection.Description;

            displayCoolingSystemCollection.SetLocation(result as PlantComponent);

            return result as CoolingGroup;
        }
    }
}

