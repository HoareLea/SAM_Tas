using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatingGroup ToTPD(this DisplayHeatingSystemCollection displayHeatingSystemCollection, PlantRoom plantRoom)
        {
            if (displayHeatingSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddHeatingGroup();
            result.Name = displayHeatingSystemCollection.Name;
            result.Description = displayHeatingSystemCollection.Description;

            displayHeatingSystemCollection.SetLocation(result as PlantComponent);

            return result as HeatingGroup;
        }
    }
}
