﻿using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DryCooler ToTPD(this DisplaySystemDryCooler displaySystemDryCooler, PlantRoom plantRoom)
        {
            if (displaySystemDryCooler == null || plantRoom == null)
            {
                return null;
            }

            DryCooler result = plantRoom.AddDryCooler();

            dynamic @dynamic = result;
            dynamic.Name = displaySystemDryCooler.Name;
            @dynamic.Description = displaySystemDryCooler.Description;

            result.DesignPressureDrop = displaySystemDryCooler.DesignPressureDrop;
            result.Capacity = displaySystemDryCooler.Capacity;
            result.CoolingSetpoint?.Update(displaySystemDryCooler.CoolingSetpoint);
            result.MaxFlowRate?.Update(displaySystemDryCooler.MaxFlowRate);
            result.FanSFP?.Update(displaySystemDryCooler.FanSFP);
            result.DryCoolerExchType = displaySystemDryCooler.DryCoolerExchangerCalculationMethod.ToTPD();
            result.Efficiency?.Update(displaySystemDryCooler.Efficiency);
            result.HeatTransSurfArea = displaySystemDryCooler.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemDryCooler.HeatTransferCoefficient;
            result.ExchangerType = displaySystemDryCooler.ExchangerType.ToTPD();
            result.AllowHeating = displaySystemDryCooler.AllowHeating.ToTPD();
            result.HeatingSetpoint?.Update(displaySystemDryCooler.HeatingSetpoint);
            result.MinSetPDeltaTCooling = displaySystemDryCooler.MinSetpointTemperatureDifferenceCooling;
            result.MinSetPDeltaTHeating = displaySystemDryCooler.MinSetpointTemperatureDifferenceHeating;
            result.HasPreCooling = displaySystemDryCooler.HasPreCooling.ToTPD();
            result.PreCoolEffectiveness?.Update(displaySystemDryCooler.PreCoolingEffectiveness);
            result.AncillaryLoad?.Update(displaySystemDryCooler.AncillaryLoad);
            result.PreCoolWaterFlowCap?.Update(displaySystemDryCooler.PreCoolingWaterFlowCapacity);
            result.MinAirFlowRate = displaySystemDryCooler.MinAirFlowRate;
            result.MinAFRRatio = displaySystemDryCooler.MinAirFlowRatio;
            result.VariableFans = displaySystemDryCooler.VariableFans.ToTPD();
            result.DesignExternalDB = displaySystemDryCooler.ExternalDryBulbTemperature;
            //result.DesignExternalDBSource = displaySystemDryCooler.ExternalDryBulbTemperatureSizingType.ToTPD();
            result.LimitingDB = displaySystemDryCooler.LimitingDryBulbTemperature;
            result.DesignRange = displaySystemDryCooler.DesignRange;
            result.DesignWaterFlowRate = displaySystemDryCooler.DesignWaterFlowRate;
            //result.WaterFlowSizingType = displaySystemDryCooler.DesignWaterFlowRateSizingType.ToTPD();

            Modify.SetSchedule((PlantComponent)result, displaySystemDryCooler.ScheduleName);

            displaySystemDryCooler.SetLocation(result as PlantComponent);

            return result;
        }
    }
}
