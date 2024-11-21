using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static RefrigerantGroup ToTPD(this DisplayRefrigerantSystemCollection displayRefrigerantSystemCollection, PlantRoom plantRoom)
        {
            if (displayRefrigerantSystemCollection == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddRefrigerantGroup();
            result.Name = displayRefrigerantSystemCollection.Name;
            result.Description = displayRefrigerantSystemCollection.Description;

            displayRefrigerantSystemCollection.SetLocation(result as PlantComponent);

            return result as RefrigerantGroup;
        }
    }
}
