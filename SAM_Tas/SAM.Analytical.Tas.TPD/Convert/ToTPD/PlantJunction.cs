using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PlantJunction ToTPD(this DisplaySystemLiquidJunction displaySystemLiquidJunction, global::TPD.PlantRoom plantRoom)
        {
            if (displaySystemLiquidJunction == null || plantRoom == null)
            {
                return null;
            }

            PlantJunction result = plantRoom.AddJunction();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemLiquidJunction.Name;
            @dynamic.Description = displaySystemLiquidJunction.Description;

            result.MainsPressure = displaySystemLiquidJunction.MainsPressure;

            displaySystemLiquidJunction.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
