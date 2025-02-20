using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemTank ToSAM(this Tank tank)
        {
            if (tank == null)
            {
                return null;
            }

            dynamic @dynamic = tank;

            SystemTank result = new SystemTank(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            result.DefinedHeatLossRate = tank.DefinedHeatLoss;
            result.InsulationConductivity = tank.InsConductivity;
            result.InsulationThickness = tank.InsThickness;
            result.Volume = tank.Volume;
            result.HeatExchangeEfficiency1 = tank.HeatExEff1;
            result.HeatExchangeEfficiency2 = tank.HeatExEff2;
            result.Height = tank.Height;
            result.AmbientTemperature = tank.AmbTemp?.ToSAM();
            result.Setpoint = tank.Setpoint.ToSAM();
            result.Capacity1 = tank.Capacity1;
            result.Capacity2 = tank.Capacity2;
            result.Capacity3 = tank.Capacity3;
            result.DesignPressureDrop1 = tank.DesignPressureDrop1;
            result.DesignPressureDrop2 = tank.DesignPressureDrop2;
            result.DesignPressureDrop3 = tank.DesignPressureDrop3;

            result.SetpointMode = tank.SetpointMethod.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            tpdTankFlags tpdTankFlags = (tpdTankFlags)tank.Flags;
            result.UseDefinedHeatLoss = tpdTankFlags.HasFlag(tpdTankFlags.tpdTankUseDefinedHeatLoss);

            DisplaySystemTank displaySystemTank = Systems.Create.DisplayObject<DisplaySystemTank>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemTank != null)
            {
                ITransform2D transform2D = ((IPlantComponent)tank).Transform2D();
                if (transform2D != null)
                {
                    displaySystemTank.Transform(transform2D);
                }

                result = displaySystemTank;
            }

            return result;
        }
    }
}
