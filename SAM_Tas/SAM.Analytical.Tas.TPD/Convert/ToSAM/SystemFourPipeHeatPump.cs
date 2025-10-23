using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFourPipeHeatPump ToSAM(this FourPipeHeatPump fourPipeHeatPump)
        {
            if (fourPipeHeatPump == null)
            {
                return null;
            }

            dynamic @dynamic = fourPipeHeatPump;

            SystemFourPipeHeatPump result = new SystemFourPipeHeatPump(@dynamic.Name)
            {
                HeatingSetpoint = ((ProfileData)@dynamic.HeatingSetpoint)?.ToSAM(),
                CoolingSetpoint = ((ProfileData)@dynamic.CoolingSetpoint)?.ToSAM(),
                HeatingEfficiency = ((ProfileData)@dynamic.HeatingEfficiency)?.ToSAM(),
                CoolingEfficiency = ((ProfileData)@dynamic.CoolingEfficiency)?.ToSAM(),
                HeatingDuty = ((SizedVariable)@dynamic.HeatingDuty)?.ToSAM(),
                CoolingDuty = ((SizedVariable)@dynamic.CoolingDuty)?.ToSAM(),
                Capacity_1 = @dynamic.Capacity1,
                DesignPressureDrop_1 = @dynamic.DesignPressureDrop1,
                DesignTemperatureDifference_1 = @dynamic.DesignDeltaT1,
                Capacity_2 = @dynamic.Capacity2,
                DesignPressureDrop_2 = @dynamic.DesignPressureDrop2,
                DesignTemperatureDifference_2 = @dynamic.DesignDeltaT2,
                MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM(),
                AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                ADFHeatingMode = @dynamic.ADFHeatMode,
                ADFCoolingMode = @dynamic.ADFCoolMode,
            };

            tpdFourPipeFlags tpdFourPipeFlags = (tpdFourPipeFlags)fourPipeHeatPump.Flags;

            List<FuelSource> fuelSources = Query.FuelSources(fourPipeHeatPump as PlantComponent);
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

            DisplaySystemFourPipeHeatPump displaySystemFourPipeHeatPump = Systems.Create.DisplayObject<DisplaySystemFourPipeHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemFourPipeHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)fourPipeHeatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemFourPipeHeatPump.Transform(transform2D);
                }

                result = displaySystemFourPipeHeatPump;
            }

            return result;
        }
    }
}