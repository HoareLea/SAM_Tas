using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingTower ToSAM(this CoolingTower coolingTower)
        {
            if (coolingTower == null)
            {
                return null;
            }

            dynamic @dynamic = coolingTower;

            SystemCoolingTower result = new SystemCoolingTower(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(coolingTower as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if(fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[1]?.Name);
                }
            }

            result.Description = dynamic.Description;
            result.Capacity = @dynamic.Capacity;
            result.DesignPressureDrop = @dynamic.DesignPressureDrop;
            result.Setpoint = @dynamic.Setpoint.ToSAM();
            result.MinApproach = @dynamic.MinApproach;
            result.VariableFans = @dynamic.VariableFans == 1;
            result.FanSFP = @dynamic.FanSFP.ToSAM();
            result.HeatTransferCoefficient = @dynamic.HeatTransCoeff;
            result.HeatTransferSurfaceArea = @dynamic.HeatTransSurfArea?.ToSAM();
            result.LimitingWetBulbTemperature = @dynamic.LimitingWetbulb;
            result.DesignApproach = @dynamic.DesignApproach;
            result.DesignRange = @dynamic.DesignRange;
            result.DesignWaterFlowRate = @dynamic.DesignWaterFlowRate;
            result.MaxAirFlowRate = @dynamic.MaxAirFlowRate?.ToSAM();
            result.FanLoadRatio = @dynamic.FanLoadRatio;
            result.AirWaterFlowRatio = @dynamic.AirWaterFlowRatio;
            result.MinAirFlowRate = @dynamic.MinAirFlowRate;
            result.FanMode2Ratio = @dynamic.FanMode2Ratio;
            result.WaterDriftLoss = @dynamic.WaterDriftLoss;
            result.BlowdownConcentrationRatio = @dynamic.BlowdownConcentrationRatio;
            result.AncillaryLoad = @dynamic.AncillaryLoad?.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCoolingTower displaySystemCoolingTower = Systems.Create.DisplayObject<DisplaySystemCoolingTower>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemCoolingTower != null)
            {
                ITransform2D transform2D = ((IPlantComponent)coolingTower).Transform2D();
                if (transform2D != null)
                {
                    displaySystemCoolingTower.Transform(transform2D);
                }

                result = displaySystemCoolingTower;
            }

            return result;
        }
    }
}
