using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FuelGroup ToTPD(this DisplayFuelSystemCollection displayFuelSystemCollection, PlantRoom plantRoom)
        {
            if (displayFuelSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddElectricalGroup();
            result.Name = displayFuelSystemCollection.Name;
            result.Description = displayFuelSystemCollection.Description;

            displayFuelSystemCollection.SetLocation(result as PlantComponent);

            return result as FuelGroup;
        }
    }
}
