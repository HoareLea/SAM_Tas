using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemWaterToWaterHeatPump ToSAM(this WaterToWaterHeatPump waterToWaterHeatPump)
        {
            if (waterToWaterHeatPump == null)
            {
                return null;
            }

            dynamic @dynamic = waterToWaterHeatPump;

            SystemWaterToWaterHeatPump result = new SystemWaterToWaterHeatPump(@dynamic.Name)
            {
                HeatingSetpoint = ((ProfileData)@dynamic.HeatingSetpoint)?.ToSAM(),
                CoolingSetpoint = ((ProfileData)@dynamic.CoolingSetpoint)?.ToSAM(),
                HeatingEfficiency = ((ProfileData)@dynamic.HeatingEfficiency)?.ToSAM(),
                CoolingEfficiency = ((ProfileData)@dynamic.CoolingEfficiency)?.ToSAM(),
                HeatingDuty = ((SizedVariable)@dynamic.HeatingDuty)?.ToSAM(),
                CoolingDuty = ((SizedVariable)@dynamic.CoolingDuty)?.ToSAM(),
                Capacity1 = @dynamic.Capacity1,
                DesignPressureDrop1 = @dynamic.DesignPressureDrop1,
                DesignTemperatureDifference1 = @dynamic.DesignDeltaT1,
                Capacity2 = @dynamic.Capacity2,
                DesignPressureDrop2 = @dynamic.DesignPressureDrop2,
                DesignTemperatureDifference2 = @dynamic.DesignDeltaT2,
                MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM(),
                AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                LossesInSizing = @dynamic.LossesInSizing,
                ScheduleName = ((dynamic)waterToWaterHeatPump)?.GetSchedule()?.Name
            };

            tpdWaterToWaterHeatPumpFlags tpdWaterToWaterHeatPumpFlags = (tpdWaterToWaterHeatPumpFlags)waterToWaterHeatPump.Flags;
            result.IsDomesticHotWater = tpdWaterToWaterHeatPumpFlags.HasFlag(tpdWaterToWaterHeatPumpFlags.tpdWaterToWaterHeatPumpIsDHW);

            List<FuelSource> fuelSources = Query.FuelSources(waterToWaterHeatPump as PlantComponent);
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

            DisplaySystemWaterToWaterHeatPump displaySystemWaterToWaterHeatPump = Systems.Create.DisplayObject<DisplaySystemWaterToWaterHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemWaterToWaterHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)waterToWaterHeatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemWaterToWaterHeatPump.Transform(transform2D);
                }

                result = displaySystemWaterToWaterHeatPump;
            }

            return result;
        }
    }
}