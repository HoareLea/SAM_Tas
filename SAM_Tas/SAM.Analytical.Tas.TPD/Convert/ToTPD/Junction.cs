using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PlantJunction ToTPD(this DisplaySystemLiquidJunction displaySystemLiquidJunction, global::TPD.PlantRoom plantRoom)
        {
            if(displaySystemLiquidJunction == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddJunction();
            result.Name = displaySystemLiquidJunction.Name;
            result.Description = displaySystemLiquidJunction.Description;

            displaySystemLiquidJunction.SetLocation(result as PlantComponent);

            return result as PlantJunction;
        }
    }
}
