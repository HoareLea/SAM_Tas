using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemChiller ToSAM(this AbsorptionChiller absorptionChiller)
        {
            if (absorptionChiller == null)
            {
                return null;
            }

            bool waterSource = absorptionChiller.IsWaterSource == -1;

            dynamic @dynamic = absorptionChiller;

            SystemChiller systemChiller = null;
            if (waterSource)
            {
                systemChiller = new SystemWaterSourceAbsorptionChiller(@dynamic.Name)
                {
                    Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM(),
                    Efficiency = ((ProfileData)@dynamic.Efficiency)?.ToSAM(),
                    Capacity1 = @dynamic.Capacity1,
                    Capacity2 = @dynamic.Capacity2,
                    Capacity3 = @dynamic.Capacity3,
                    DesignPressureDrop1 = @dynamic.DesignPressureDrop1,
                    DesignPressureDrop2 = @dynamic.DesignPressureDrop2,
                    DesignPressureDrop3 = @dynamic.DesignPressureDrop3,
                    AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                    MinimalOutSourceTemperature = ((ProfileData)@dynamic.MinOutTempSource)?.ToSAM(),
                    LossesInSizing = dynamic.LossesInSizing
                };
            }
            else
            {
                systemChiller = new SystemAbsorptionChiller(@dynamic.Name)
                {
                    Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM(),
                    Efficiency = ((ProfileData)@dynamic.Efficiency)?.ToSAM(),
                    Capacity1 = @dynamic.Capacity1,
                    Capacity2 = @dynamic.Capacity2,
                    //Capacity3 = @dynamic.Capacity3,
                    DesignPressureDrop1 = @dynamic.DesignPressureDrop1,
                    DesignPressureDrop2 = @dynamic.DesignPressureDrop2,
                    //DesignPressureDrop3 = @dynamic.DesignPressureDrop3,
                    AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                    MinimalOutSourceTemperature = ((ProfileData)@dynamic.MinOutTempSource)?.ToSAM(),
                    LossesInSizing = dynamic.LossesInSizing,
                    ScheduleName = ((dynamic)systemChiller )?.GetSchedule()?.Name
                };
            }

            Modify.SetReference(systemChiller, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(absorptionChiller as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                systemChiller.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[0]?.Name);
            }

            systemChiller.Description = dynamic.Description;
            systemChiller.Duty = ((SizedVariable)dynamic.Duty)?.ToSAM();

            Point2D location = ((TasPosition)dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject result = null;
            if (waterSource)
            {
                result = Systems.Create.DisplayObject<DisplaySystemWaterSourceAbsorptionChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                result = Systems.Create.DisplayObject<DisplaySystemAbsorptionChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }

            ITransform2D transform2D = ((IPlantComponent)absorptionChiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result as SystemChiller;
        }

        public static SystemChiller ToSAM(this Chiller chiller)
        {
            if (chiller == null)
            {
                return null;
            }

            bool directAbsorptionChiller = chiller.IsDirectAbsChiller == -1;
            
            dynamic @dynamic = chiller;

            SystemChiller result = null;
            if (directAbsorptionChiller)
            {
                SystemAirSourceDirectAbsorptionChiller systemAirSourceDirectAbsorptionChiller = new SystemAirSourceDirectAbsorptionChiller(@dynamic.Name);
                systemAirSourceDirectAbsorptionChiller.Setpoint = chiller.Setpoint?.ToSAM();
                systemAirSourceDirectAbsorptionChiller.Efficiency = chiller.Efficiency?.ToSAM();
                systemAirSourceDirectAbsorptionChiller.CondenserFanLoad = chiller.CondenserFanLoad?.ToSAM();
                systemAirSourceDirectAbsorptionChiller.DesignTemperatureDifference = chiller.DesignDeltaT;
                systemAirSourceDirectAbsorptionChiller.Capacity = chiller.Capacity;
                systemAirSourceDirectAbsorptionChiller.DesignPressureDrop = chiller.DesignPressureDrop;
                systemAirSourceDirectAbsorptionChiller.LossesInSizing = dynamic.LossesInSizing;

                systemAirSourceDirectAbsorptionChiller.ScheduleName = ((dynamic)chiller )?.GetSchedule()?.Name;


                result = systemAirSourceDirectAbsorptionChiller;
            }
            else
            {
                SystemAirSourceChiller systemAirSourceChiller = new SystemAirSourceChiller(@dynamic.Name);
                systemAirSourceChiller.Setpoint = chiller.Setpoint?.ToSAM();
                systemAirSourceChiller.Efficiency = chiller.Efficiency?.ToSAM();
                systemAirSourceChiller.CondenserFanLoad = chiller.CondenserFanLoad?.ToSAM();
                systemAirSourceChiller.DesignTemperatureDifference = chiller.DesignDeltaT;
                systemAirSourceChiller.Capacity = chiller.Capacity;
                systemAirSourceChiller.DesignPressureDrop = chiller.DesignPressureDrop;
                systemAirSourceChiller.LossesInSizing = dynamic.LossesInSizing;

                systemAirSourceChiller.ScheduleName = ((dynamic)chiller )?.GetSchedule()?.Name;

                result = systemAirSourceChiller;
            }

            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(chiller as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if (fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[1]?.Name);
                }
            }

            result.Description = dynamic.Description;
            result.Duty = ((SizedVariable)dynamic.Duty)?.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject displaySystemObject = null;
            if (directAbsorptionChiller)
            {
                displaySystemObject = Systems.Create.DisplayObject<DisplaySystemAirSourceDirectAbsorptionChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                displaySystemObject = Systems.Create.DisplayObject<DisplaySystemAirSourceChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            }

            if(displaySystemObject != null)
            {
                ITransform2D transform2D = ((IPlantComponent)chiller).Transform2D();
                if (transform2D != null)
                {
                    displaySystemObject.Transform(transform2D);
                }

                result = displaySystemObject as SystemChiller;
            }

            return result;
        }

        public static SystemChiller ToSAM(this WaterSourceChiller waterSourceChiller)
        {
            if (waterSourceChiller == null)
            {
                return null;
            }

            bool directAbsorptionChiller = waterSourceChiller.IsDirectAbsChiller == -1;

            dynamic @dynamic = waterSourceChiller;

            SystemChiller result = null;
            if (directAbsorptionChiller)
            {
                SystemWaterSourceDirectAbsorptionChiller systemWaterSourceDirectAbsorptionChiller = new SystemWaterSourceDirectAbsorptionChiller(@dynamic.Name);
                systemWaterSourceDirectAbsorptionChiller.Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM();
                systemWaterSourceDirectAbsorptionChiller.Efficiency = ((ProfileData)@dynamic.Efficiency)?.ToSAM();
                systemWaterSourceDirectAbsorptionChiller.Capacity1 = @dynamic.Capacity1;
                systemWaterSourceDirectAbsorptionChiller.Capacity2 = @dynamic.Capacity2;
                systemWaterSourceDirectAbsorptionChiller.DesignPressureDrop1 = @dynamic.DesignPressureDrop1;
                systemWaterSourceDirectAbsorptionChiller.DesignPressureDrop2 = @dynamic.DesignPressureDrop2;
                systemWaterSourceDirectAbsorptionChiller.DesignTemperatureDifference1 = @dynamic.DesignDeltaT1;
                systemWaterSourceDirectAbsorptionChiller.DesignTemperatureDifference2 = @dynamic.DesignDeltaT2;
                systemWaterSourceDirectAbsorptionChiller.LossesInSizing = dynamic.LossesInSizing;
                systemWaterSourceDirectAbsorptionChiller.MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM();

                systemWaterSourceDirectAbsorptionChiller.ExchangerCalculationMethod = ((tpdExchangerCalcMethod)@dynamic.ExchCalcType).ToSAM();
                systemWaterSourceDirectAbsorptionChiller.ExchangerEfficiency = ((ProfileData)@dynamic.ExchangerEfficiency)?.ToSAM();
                systemWaterSourceDirectAbsorptionChiller.HeatTransferSurfaceArea = dynamic.HeatTransSurfArea;
                systemWaterSourceDirectAbsorptionChiller.HeatTransferCoefficient = dynamic.HeatTransCoeff;
                systemWaterSourceDirectAbsorptionChiller.ExchangerType = ((tpdExchangerType)@dynamic.ExchangerType).ToSAM();
                systemWaterSourceDirectAbsorptionChiller.AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM();
                systemWaterSourceDirectAbsorptionChiller.FreeCoolingType = ((tpdFreeCoolingType)@dynamic.FreeCoolingType).ToSAM();

                systemWaterSourceDirectAbsorptionChiller.ScheduleName = ((dynamic)waterSourceChiller )?.GetSchedule()?.Name;

                result = systemWaterSourceDirectAbsorptionChiller;
            }
            else
            {
                SystemWaterSourceChiller systemWaterSourceChiller = new SystemWaterSourceChiller(@dynamic.Name);
                systemWaterSourceChiller.Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM();
                systemWaterSourceChiller.Efficiency = ((ProfileData)@dynamic.Efficiency)?.ToSAM();
                systemWaterSourceChiller.Capacity1 = @dynamic.Capacity1;
                systemWaterSourceChiller.DesignPressureDrop1 = @dynamic.DesignPressureDrop1;
                systemWaterSourceChiller.DesignTemperatureDifference1 = @dynamic.DesignDeltaT1;
                systemWaterSourceChiller.Capacity2 = @dynamic.Capacity2;
                systemWaterSourceChiller.DesignPressureDrop2 = @dynamic.DesignPressureDrop2;
                systemWaterSourceChiller.DesignTemperatureDifference2 = @dynamic.DesignDeltaT2;
                systemWaterSourceChiller.LossesInSizing = dynamic.LossesInSizing;
                systemWaterSourceChiller.MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM();

                systemWaterSourceChiller.ExchangerCalculationMethod = ((tpdExchangerCalcMethod)@dynamic.ExchCalcType).ToSAM();
                systemWaterSourceChiller.ExchangerEfficiency = ((ProfileData)@dynamic.ExchangerEfficiency)?.ToSAM();
                systemWaterSourceChiller.HeatTransferSurfaceArea = dynamic.HeatTransSurfArea;
                systemWaterSourceChiller.HeatTransferCoefficient = dynamic.HeatTransCoeff;
                systemWaterSourceChiller.ExchangerType = ((tpdExchangerType)@dynamic.ExchangerType).ToSAM();
                systemWaterSourceChiller.AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM();
                systemWaterSourceChiller.FreeCoolingType = ((tpdFreeCoolingType)@dynamic.FreeCoolingType).ToSAM();

                systemWaterSourceChiller.ScheduleName = ((dynamic)waterSourceChiller)?.GetSchedule()?.Name;

                result = systemWaterSourceChiller;
            }

            result.Description = dynamic.Description;

            result.Duty = ((SizedVariable)dynamic.Duty)?.ToSAM();


            List<FuelSource> fuelSources = Query.FuelSources(waterSourceChiller as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                if (fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[1]?.Name);
                }
            }

            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject displaySystemObject = null;
            if (directAbsorptionChiller)
            {
                displaySystemObject = Systems.Create.DisplayObject<DisplaySystemWaterSourceDirectAbsorptionChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                displaySystemObject = Systems.Create.DisplayObject<DisplaySystemWaterSourceChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            }

            if(displaySystemObject != null)
            {
                ITransform2D transform2D = ((IPlantComponent)waterSourceChiller).Transform2D();
                if (transform2D != null)
                {
                    displaySystemObject.Transform(transform2D);
                }

                result = displaySystemObject as SystemChiller;
            }

            return result;
        }

        public static SystemChiller ToSAM(this IceStorageChiller iceStorageChiller)
        {
            if (iceStorageChiller == null)
            {
                return null;
            }

            bool waterSource = iceStorageChiller.IsWaterSource == -1;

            dynamic @dynamic = iceStorageChiller;

            List<FuelSource> fuelSources = Query.FuelSources(iceStorageChiller as PlantComponent);

            SystemChiller result = null;
            if (waterSource)
            {
                SystemWaterSourceIceStorageChiller systemWaterSourceIceStorageChiller = new SystemWaterSourceIceStorageChiller(@dynamic.Name)
                {
                    Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM(),
                    Efficiency = ((ProfileData)@dynamic.Efficiency)?.ToSAM(),
                    IceMakingEfficiency = ((ProfileData)@dynamic.IceMakingEfficiency)?.ToSAM(),
                    Capacity1 = @dynamic.Capacity1,
                    DesignPressureDrop1 = @dynamic.DesignPressureDrop1,
                    DesignTemperatureDifference1 = @dynamic.DesignDeltaT1,
                    Capacity2 = @dynamic.Capacity2,
                    DesignPressureDrop2 = @dynamic.DesignPressureDrop2,
                    DesignTemperatureDifference2 = @dynamic.DesignDeltaT2,
                    IceCapacity = ((SizedVariable)dynamic.IceCapacity)?.ToSAM(),
                    InitialIceReserve = @dynamic.InitialIceReserve,
                    CondenserFanLoad = ((ProfileData)@dynamic.CondenserFanLoad)?.ToSAM(),
                    MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM(),
                    IceMeltChillerFraction = @dynamic.IceMeltChillerFraction,
                    AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                    LossesInSizing = dynamic.LossesInSizing
                };

                systemWaterSourceIceStorageChiller.ScheduleName = ((dynamic)iceStorageChiller)?.GetSchedule()?.Name;

                if (fuelSources != null && fuelSources.Count > 0)
                {
                    systemWaterSourceIceStorageChiller.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                    if (fuelSources.Count > 2)
                    {
                        systemWaterSourceIceStorageChiller.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[2]?.Name);
                    }
                }

                result = systemWaterSourceIceStorageChiller;
            }
            else
            {
                SystemIceStorageChiller systemIceStorageChiller = new SystemIceStorageChiller(@dynamic.Name)
                {
                    Setpoint = ((ProfileData)@dynamic.Setpoint)?.ToSAM(),
                    Efficiency = ((ProfileData)@dynamic.Efficiency)?.ToSAM(),
                    IceMakingEfficiency = ((ProfileData)@dynamic.IceMakingEfficiency)?.ToSAM(),
                    Capacity = @dynamic.Capacity1,
                    DesignPressureDrop = @dynamic.DesignPressureDrop1,
                    DesignTemperatureDifference = @dynamic.DesignDeltaT1,
                    IceCapacity = ((SizedVariable)dynamic.IceCapacity)?.ToSAM(),
                    InitialIceReserve = @dynamic.InitialIceReserve,
                    CondenserFanLoad = ((ProfileData)@dynamic.CondenserFanLoad)?.ToSAM(),
                    MotorEfficiency = ((ProfileData)@dynamic.MotorEfficiency)?.ToSAM(),
                    IceMeltChillerFraction = @dynamic.IceMeltChillerFraction,
                    AncillaryLoad = ((ProfileData)@dynamic.AncillaryLoad)?.ToSAM(),
                    LossesInSizing = dynamic.LossesInSizing
                };

                if (fuelSources != null && fuelSources.Count > 0)
                {
                    systemIceStorageChiller.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                    if (fuelSources.Count > 1)
                    {
                        systemIceStorageChiller.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[1]?.Name);
                        if (fuelSources.Count > 2)
                        {
                            systemIceStorageChiller.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[2]?.Name);
                        }
                    }
                }

                result = systemIceStorageChiller;
            }

            result.Description = dynamic.Description;
            result.Duty = ((SizedVariable)dynamic.Duty)?.ToSAM();
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject displaySystemObject = null;
            if (waterSource)
            {
                displaySystemObject = Systems.Create.DisplayObject<DisplaySystemWaterSourceIceStorageChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                displaySystemObject = Systems.Create.DisplayObject<DisplaySystemIceStorageChiller>(result, location, Systems.Query.DefaultDisplaySystemManager());
            }

            if(displaySystemObject != null)
            {
                ITransform2D transform2D = ((IPlantComponent)iceStorageChiller).Transform2D();
                if (transform2D != null)
                {
                    displaySystemObject.Transform(transform2D);
                }

                result = displaySystemObject as SystemChiller;
            }

            return result; 
        }
    }
}
