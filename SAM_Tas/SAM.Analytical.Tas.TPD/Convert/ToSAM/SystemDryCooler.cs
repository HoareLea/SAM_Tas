using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDryCooler ToSAM(this DryCooler dryCooler)
        {
            if (dryCooler == null)
            {
                return null;
            }

            dynamic @dynamic = dryCooler;

            SystemDryCooler result = new SystemDryCooler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(dryCooler as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[0]?.Name);
                if (fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[1]?.Name);
                }
            }

            result.Description = dynamic.Description;

            result.DesignPressureDrop = dynamic.DesignPressureDrop;
            result.Capacity = dynamic.Capacity;
            result.CoolingSetpoint = ((ProfileData)dynamic.CoolingSetpoint)?.ToSAM();
            result.MaxFlowRate = ((SizedVariable)dynamic.MaxFlowRate)?.ToSAM();
            result.FanSFP = ((ProfileData)dynamic.FanSFP)?.ToSAM();
            result.DryCoolerExchangerCalculationMethod = ((tpdExchangerCalcMethod)dynamic.DryCoolerExchType).ToSAM();
            result.Efficiency = ((ProfileData)dynamic.Efficiency)?.ToSAM();
            result.HeatTransferSurfaceArea = dynamic.HeatTransSurfArea;
            result.HeatTransferCoefficient = dynamic.HeatTransCoeff;
            result.ExchangerType = ((tpdExchangerType)dynamic.ExchangerType).ToSAM();
            result.AllowHeating = dynamic.AllowHeating;
            result.HeatingSetpoint = ((ProfileData)dynamic.HeatingSetpoint)?.ToSAM();
            result.MinSetpointTemperatureDifferenceCooling = dynamic.MinSetPDeltaTCooling;
            result.MinSetpointTemperatureDifferenceHeating = dynamic.MinSetPDeltaTHeating;
            result.HasPreCooling = dynamic.HasPreCooling;
            result.PreCoolingEffectiveness = ((ProfileData)dynamic.PreCoolEffectiveness)?.ToSAM();
            result.AncillaryLoad = ((ProfileData)dynamic.AncillaryLoad)?.ToSAM();
            result.PreCoolingWaterFlowCapacity = ((SizedVariable)dynamic.PreCoolWaterFlowCap)?.ToSAM();
            result.MinAirFlowRate = dynamic.MinAirFlowRate;
            result.MinAirFlowRatio = dynamic.MinAFRRatio;
            result.VariableFans = dynamic.VariableFans;
            result.ExternalDryBulbTemperature = dynamic.DesignExternalDB;
            result.ExternalDryBulbTemperatureSizingType = ((tpdSizedVariable)dynamic.DesignExternalDBSource).ToSAM_TemperatureSizingType();
            result.LimitingDryBulbTemperature = dynamic.LimitingDB;
            result.DesignRange = dynamic.DesignRange;
            result.DesignWaterFlowRate = dynamic.DesignWaterFlowRate;
            result.DesignWaterFlowRateSizingType = ((tpdSizedVariable)dynamic.WaterFlowSizingType).ToSAM_DesignWaterFlowRateSizingType();

            result.ScheduleName = ((dynamic)dryCooler )?.GetSchedule()?.Name;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDryCooler displaySystemDryCooler = Systems.Create.DisplayObject<DisplaySystemDryCooler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemDryCooler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)dryCooler).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDryCooler.Transform(transform2D);
                }

                result = displaySystemDryCooler;
            }

            return result;
        }
    }
}



