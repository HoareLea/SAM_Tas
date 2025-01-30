using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLiquidExchanger ToSAM(this HeatExchanger heatExchanger)
        {
            if (heatExchanger == null)
            {
                return null;
            }

            dynamic @dynamic = heatExchanger;

            SystemLiquidExchanger result = new SystemLiquidExchanger(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            
            result.Efficiency = ((ProfileData)@dynamic.Efficiency).ToSAM();

            result.Capacity1 = dynamic.Capacity1;
            result.Capacity2 = dynamic.Capacity2;

            result.DesignPressureDrop1 = @dynamic.DesignPressureDrop1;
            result.DesignPressureDrop2 = @dynamic.DesignPressureDrop2;

            result.BypassPosition = ((tpdExchangerPosition)@dynamic.BypassPosition).ToSAM();

            result.Setpoint = ((ProfileData)@dynamic.Setpoint).ToSAM();

            result.SetpointPosition = ((tpdExchangerPosition)@dynamic.SetpointPosition).ToSAM();

            //result.Setpoint2 = ((ProfileData)@dynamic.Setpoint2).ToSAM();

            result.ExchangerCalculationMethod = ((tpdExchangerCalcMethod)@dynamic.ExchCalcType).ToSAM();

            result.ExchangerType = ((tpdExchangerType)@dynamic.ExchangerType).ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            result.HeatTransferSurfaceArea = @dynamic.HeatTransSurfArea;
            result.HeatTransferCoefficient = @dynamic.HeatTransCoeff;

            result.ScheduleName = ((dynamic)heatExchanger )?.GetSchedule()?.Name;

            DisplaySystemLiquidExchanger displaySystemLiquidExchanger = Systems.Create.DisplayObject<DisplaySystemLiquidExchanger>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemLiquidExchanger != null)
            {
                ITransform2D transform2D = ((IPlantComponent)heatExchanger).Transform2D();
                if (transform2D != null)
                {
                    displaySystemLiquidExchanger.Transform(transform2D);
                }

                result = displaySystemLiquidExchanger;
            }

            return result;
        }
    }
}

