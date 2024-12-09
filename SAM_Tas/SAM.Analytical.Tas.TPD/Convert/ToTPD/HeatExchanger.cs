using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatExchanger ToTPD(this DisplaySystemLiquidExchanger displaySystemLiquidExchanger, PlantRoom plantRoom)
        {
            if (displaySystemLiquidExchanger == null || plantRoom == null)
            {
                return null;
            }

            dynamic result = plantRoom.AddHeatExchanger();
            result.Name = displaySystemLiquidExchanger.Name;
            result.Description = displaySystemLiquidExchanger.Description;

            displaySystemLiquidExchanger.SetLocation(result as PlantComponent);

            return result as HeatExchanger;
        }
    }
}
