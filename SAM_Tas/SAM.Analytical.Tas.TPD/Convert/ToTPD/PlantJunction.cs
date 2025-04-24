using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static PlantJunction ToTPD(this DisplaySystemLiquidJunction displaySystemLiquidJunction, PlantRoom plantRoom, PlantJunction plantJunction = null)
        {
            if (displaySystemLiquidJunction == null || plantRoom == null)
            {
                return null;
            }

            PlantJunction result = plantJunction;
            if(result == null)
            {
                result = plantRoom.AddJunction();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemLiquidJunction.Name;
            @dynamic.Description = displaySystemLiquidJunction.Description;

            result.MainsPressure = displaySystemLiquidJunction.MainsPressure;

            if(plantJunction == null)
            {
                displaySystemLiquidJunction.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
