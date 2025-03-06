using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Tank ToTPD(this DisplaySystemTank displaySystemTank, PlantRoom plantRoom, Tank tank = null)
        {
            if (displaySystemTank == null || plantRoom == null)
            {
                return null;
            }

            Tank result = tank;
            if(result == null)
            {
                result = plantRoom.AddTank();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemTank.Name;
            @dynamic.Description = displaySystemTank.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.DefinedHeatLoss = displaySystemTank.DefinedHeatLossRate;
            result.InsConductivity = displaySystemTank.InsulationConductivity;
            result.InsThickness = displaySystemTank.InsulationThickness;
            result.Volume = displaySystemTank.Volume;
            result.HeatExEff1 = displaySystemTank.HeatExchangeEfficiency1;
            result.HeatExEff2 = displaySystemTank.HeatExchangeEfficiency2;
            result.Height = displaySystemTank.Height;
            result.AmbTemp?.Update(displaySystemTank.AmbientTemperature, energyCentre);
            result.SetpointMethod = displaySystemTank.SetpointMode.ToTPD();
            result.Setpoint.Update(displaySystemTank.Setpoint, energyCentre);
            result.Capacity1 = displaySystemTank.Capacity1;
            result.Capacity2 = displaySystemTank.Capacity2;
            result.Capacity3 = displaySystemTank.Capacity3;
            result.DesignPressureDrop1 = displaySystemTank.DesignPressureDrop1;
            result.DesignPressureDrop2 = displaySystemTank.DesignPressureDrop2;
            result.DesignPressureDrop3 = displaySystemTank.DesignPressureDrop3;

            if(displaySystemTank.UseDefinedHeatLoss)
            {
                result.Flags = (int)tpdTankFlags.tpdTankUseDefinedHeatLoss;
            }

            if(tank == null)
            {
                displaySystemTank.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
