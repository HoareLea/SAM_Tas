using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static HeatExchanger ToTPD(this DisplaySystemLiquidExchanger displaySystemLiquidExchanger, PlantRoom plantRoom, HeatExchanger heatExchanger = null)
        {
            if (displaySystemLiquidExchanger == null || plantRoom == null)
            {
                return null;
            }

            HeatExchanger result = heatExchanger;
            if(result == null)
            {
                result = plantRoom.AddHeatExchanger();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemLiquidExchanger.Name;
            @dynamic.Description = displaySystemLiquidExchanger.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Efficiency?.Update(displaySystemLiquidExchanger.Efficiency, energyCentre);
            result.Capacity1 = displaySystemLiquidExchanger.Capacity1;
            result.Capacity2 = displaySystemLiquidExchanger.Capacity2;
            result.DesignPressureDrop1 = displaySystemLiquidExchanger.DesignPressureDrop1;
            result.DesignPressureDrop2 = displaySystemLiquidExchanger.DesignPressureDrop2;
            result.BypassPosition = displaySystemLiquidExchanger.BypassPosition.ToTPD();
            result.Setpoint?.Update(displaySystemLiquidExchanger.Setpoint, energyCentre);
            result.SetpointPosition = displaySystemLiquidExchanger.SetpointPosition.ToTPD();
            //result.Setpoint2?.Update(displaySystemLiquidExchanger.Setpoint2);
            result.ExchangerType = displaySystemLiquidExchanger.ExchangerType.ToTPD();
            result.HeatTransSurfArea = displaySystemLiquidExchanger.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemLiquidExchanger.HeatTransferCoefficient;

            Modify.SetSchedule((PlantComponent)result, displaySystemLiquidExchanger.ScheduleName);

            if(heatExchanger == null)
            {
                displaySystemLiquidExchanger.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
