using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirSourceHeatPump ToSAM(this AirSourceHeatPump airSourceHeatPump)
        {
            if (airSourceHeatPump == null)
            {
                return null;
            }

            dynamic @dynamic = airSourceHeatPump;

            SystemAirSourceHeatPump result = new SystemAirSourceHeatPump(@dynamic.Name)
            {
                HeatPumpType = ((tpdHeatPumpType)@dynamic.Type).ToSAM(),
                CoolingCapacity = ((SizedVariable)@dynamic.CoolingCapacity)?.ToSAM(),
                CoolingPower = ((ProfileData)@dynamic.CoolingPower)?.ToSAM(),
                HeatingCapacity = ((ProfileData)@dynamic.HeatingCapacity)?.ToSAM(),
                HeatingPower = ((ProfileData)@dynamic.HeatingPower)?.ToSAM(),
                CondenserFanLoad = ((ProfileData)@dynamic.CondenserFanLoad)?.ToSAM(),
                HeatingCoolingDutyRatio = @dynamic.HeatCoolDutyRatio,
                HeatingCapacityPowerRatio = @dynamic.HeatCapPowRatio,
                CoolingCapacityPowerRatio = @dynamic.CoolCapPowRatio,
                MaxDemandFanRatio = @dynamic.MaxDemFanRatio,
                StandbyPower = @dynamic.StandbyPower,
                ADFHeatingMode = @dynamic.ADFHeatMode,
                ADFCoolingMode = @dynamic.ADFCoolMode,
                PortHeatingPower = @dynamic.PowHeatPort,
                PortCoolingPower = @dynamic.PowCoolPort,
                WaterPipeLength = @dynamic.WaterPipeLength,
                AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                HeatSizeFraction = @dynamic.HeatSizeFraction,
                ScheduleName = ((dynamic)airSourceHeatPump )?.GetSchedule()?.Name
            };

            tpdAirSourceHeatPumpFlags tpdAirSourceHeatPumpFlags = (tpdAirSourceHeatPumpFlags)airSourceHeatPump.Flags;
            result.IsDomesticHotWater = tpdAirSourceHeatPumpFlags.HasFlag(tpdAirSourceHeatPumpFlags.tpdAirSourceHeatPumpIsDHW);


            List<FuelSource> fuelSources = Query.FuelSources(airSourceHeatPump as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if (fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[1]?.Name);
                    if (fuelSources.Count > 2)
                    {
                        result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[2]?.Name);
                    }
                }
            }

            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemAirSourceHeatPump displaySystemAirSourceHeatPump = Systems.Create.DisplayObject<DisplaySystemAirSourceHeatPump>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemAirSourceHeatPump != null)
            {
                ITransform2D transform2D = ((IPlantComponent)airSourceHeatPump).Transform2D();
                if (transform2D != null)
                {
                    displaySystemAirSourceHeatPump.Transform(transform2D);
                }

                result = displaySystemAirSourceHeatPump; 
            }

            return result;
        }
    }
}
