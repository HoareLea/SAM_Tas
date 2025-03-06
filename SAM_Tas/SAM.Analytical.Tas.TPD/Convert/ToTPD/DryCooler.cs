using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static DryCooler ToTPD(this DisplaySystemDryCooler displaySystemDryCooler, PlantRoom plantRoom, DryCooler dryCooler = null)
        {
            if (displaySystemDryCooler == null || plantRoom == null)
            {
                return null;
            }

            DryCooler result = dryCooler;
            if(result == null)
            {
                result = plantRoom.AddDryCooler();
            }

            dynamic @dynamic = result;
            dynamic.Name = displaySystemDryCooler.Name;
            @dynamic.Description = displaySystemDryCooler.Description;

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.DesignPressureDrop = displaySystemDryCooler.DesignPressureDrop;
            result.Capacity = displaySystemDryCooler.Capacity;
            result.CoolingSetpoint?.Update(displaySystemDryCooler.CoolingSetpoint, energyCentre);
            result.MaxFlowRate?.Update(displaySystemDryCooler.MaxFlowRate, plantRoom);
            result.FanSFP?.Update(displaySystemDryCooler.FanSFP, energyCentre);
            result.DryCoolerExchType = displaySystemDryCooler.DryCoolerExchangerCalculationMethod.ToTPD();
            result.Efficiency?.Update(displaySystemDryCooler.Efficiency, energyCentre);
            result.HeatTransSurfArea = displaySystemDryCooler.HeatTransferSurfaceArea;
            result.HeatTransCoeff = displaySystemDryCooler.HeatTransferCoefficient;
            result.ExchangerType = displaySystemDryCooler.ExchangerType.ToTPD();
            result.AllowHeating = displaySystemDryCooler.AllowHeating.ToTPD();
            result.HeatingSetpoint?.Update(displaySystemDryCooler.HeatingSetpoint, energyCentre);
            result.MinSetPDeltaTCooling = displaySystemDryCooler.MinSetpointTemperatureDifferenceCooling;
            result.MinSetPDeltaTHeating = displaySystemDryCooler.MinSetpointTemperatureDifferenceHeating;
            result.HasPreCooling = displaySystemDryCooler.HasPreCooling.ToTPD();
            result.PreCoolEffectiveness?.Update(displaySystemDryCooler.PreCoolingEffectiveness, energyCentre);
            result.AncillaryLoad?.Update(displaySystemDryCooler.AncillaryLoad, energyCentre);
            result.PreCoolWaterFlowCap?.Update(displaySystemDryCooler.PreCoolingWaterFlowCapacity, plantRoom);
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

            FuelSource fuelSource;

            fuelSource = plantRoom.FuelSource(displaySystemDryCooler.GetValue<string>(Core.Systems.SystemObjectParameter.FanEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(1, fuelSource);
            }

            fuelSource = plantRoom.FuelSource(displaySystemDryCooler.GetValue<string>(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName));
            if (fuelSource != null)
            {
                ((@dynamic)result).SetFuelSource(2, fuelSource);
            }

            if(dryCooler == null)
            {
                displaySystemDryCooler.SetLocation(result as PlantComponent);
            }

            return result;
        }
    }
}
