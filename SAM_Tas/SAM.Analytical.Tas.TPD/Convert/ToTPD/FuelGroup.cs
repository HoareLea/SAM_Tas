using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static FuelGroup ToTPD(this DisplayFuelSystemCollection displayFuelSystemCollection, PlantRoom plantRoom, FuelGroup fuelGroup = null)
        {
            if (displayFuelSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            FuelGroup result = fuelGroup;
            if(result == null)
            {
                result = plantRoom.AddFuelGroup();
            }

            dynamic @dynamic = result;

            @dynamic.Name = displayFuelSystemCollection.Name;
            @dynamic.Description = displayFuelSystemCollection.Description;

            if(fuelGroup == null)
            {
                displayFuelSystemCollection.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
