using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterSourceHeatPump ToSAM(this HeatPump heatPump)
        {
            if (heatPump == null)
            {
                return null;
            }

            dynamic @dynamic = heatPump;

            SystemWaterSourceHeatPump result = new SystemWaterSourceHeatPump(@dynamic.Name)
            {
                HeatPumpType = ((tpdHeatPumpType)@dynamic.Type).ToSAM(),
                CoolingCapacity = ((SizedVariable)@dynamic.CoolingCapacity)?.ToSAM(),
                CoolingPower = ((ProfileData)@dynamic.CoolingPower)?.ToSAM(),
                HeatingCapacity = ((ProfileData)@dynamic.HeatingCapacity)?.ToSAM(),
                HeatingPower = ((ProfileData)@dynamic.HeatingPower)?.ToSAM(),
                HeatingCoolingDutyRatio = @dynamic.HeatCoolDutyRatio,
                HeatingCapacityPowerRatio = @dynamic.HeatCapPowRatio,
                CoolingCapacityPowerRatio = @dynamic.CoolCapPowRatio,
                DesignPressureDrop = @dynamic.DesignPressureDrop,
                Capacity = @dynamic.Capacity,
                DesignTemperatureDifference = @dynamic.DesignDeltaT,
                StandbyPower = @dynamic.StandbyPower,
                ADFHeatingMode = @dynamic.ADFHeatMode,
                ADFCoolingMode = @dynamic.ADFCoolMode,
                PortHeatingPower = @dynamic.PowHeatPort,
                PortCoolingPower = @dynamic.PowCoolPort,
                MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM(),
                HeatSizeFraction = @dynamic.HeatSizeFraction,
                AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                ScheduleName = ((dynamic)heatPump )?.GetSchedule()?.Name
            };

            tpdHeatPumpFlags tpdHeatPumpFlags = (tpdHeatPumpFlags)heatPump.Flags;
            result.IsDomesticHotWater = tpdHeatPumpFlags.HasFlag(tpdHeatPumpFlags.tpdHeatPumpIsDHW);

            List<FuelSource> fuelSources = Query.FuelSources(heatPump as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if (fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[1]?.Name);
                }
            }

            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemWaterSourceHeatPump displaySystemWaterSourceHeatPump = Systems.Create.DisplayObject<DisplaySystemWaterSourceHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemWaterSourceHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)heatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemWaterSourceHeatPump.Transform(transform2D);
                }

                result = displaySystemWaterSourceHeatPump;

            }

            return result;
        }
    }
}
