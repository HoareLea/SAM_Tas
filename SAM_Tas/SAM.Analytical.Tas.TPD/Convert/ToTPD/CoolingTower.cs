using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CoolingTower ToTPD(this DisplaySystemCoolingTower displaySystemCoolingTower, PlantRoom plantRoom)
        {
            if (displaySystemCoolingTower == null || plantRoom == null)
            {
                return null;
            }

            CoolingTower result = plantRoom.AddCoolingTower();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemCoolingTower.Name;
            @dynamic.Description = displaySystemCoolingTower.Description;

            result.Capacity = displaySystemCoolingTower.Capacity;
            result.DesignPressureDrop = displaySystemCoolingTower.DesignPressureDrop;
            result.Setpoint?.Update(displaySystemCoolingTower.Setpoint);
            result.MinApproach = displaySystemCoolingTower.MinApproach;
            result.VariableFans = displaySystemCoolingTower.VariableFans.ToTPD();
            result.FanSFP?.Update(displaySystemCoolingTower.FanSFP);
            result.HeatTransCoeff = displaySystemCoolingTower.HeatTransferCoefficient;
            result.HeatTransSurfArea?.Update(displaySystemCoolingTower.HeatTransferSurfaceArea);
            result.DesignExternalWetbulb = displaySystemCoolingTower.ExternalWetBulbTemperature;
            //result.DesignExternalWetbulbSource = displaySystemCoolingTower.DesignWaterFlowRateSizingType;
            //result.LimitingWetbulb = displaySystemCoolingTower.limi
            result.DesignApproach = displaySystemCoolingTower.DesignApproach;
            result.DesignRange = displaySystemCoolingTower.DesignRange;
            result.DesignWaterFlowRate = displaySystemCoolingTower.DesignWaterFlowRate;
            //result.WaterFlowSizingType = displaySystemCoolingTower.MaxAirFlowRateSizingType;
            result.MaxAirFlowRate?.Update(displaySystemCoolingTower.MaxAirFlowRate);
            result.FanLoadRatio = displaySystemCoolingTower.FanLoadRatio;
            result.AirWaterFlowRatio = displaySystemCoolingTower.AirWaterFlowRatio;
            result.MinAirFlowRate = displaySystemCoolingTower.MinAirFlowRate;
            result.FanMode2Ratio = displaySystemCoolingTower.FanMode2Ratio;
            result.WaterDriftLoss = displaySystemCoolingTower.WaterDriftLoss;
            result.BlowdownConcentrationRatio = displaySystemCoolingTower.BlowdownConcentrationRatio;
            result.AncillaryLoad?.Update(displaySystemCoolingTower.AncillaryLoad);

            Modify.SetSchedule((PlantComponent)result, displaySystemCoolingTower.ScheduleName);

            displaySystemCoolingTower.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
