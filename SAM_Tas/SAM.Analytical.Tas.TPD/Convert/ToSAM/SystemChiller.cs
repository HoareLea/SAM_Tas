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
                    DesignPressureDrop1 = absorptionChiller.DesignPressureDrop1,
                    DesignPressureDrop2 = absorptionChiller.DesignPressureDrop2,
                    DesignPressureDrop3 = absorptionChiller.DesignPressureDrop3,
                };
            }
            else
            {
                systemChiller = new SystemAbsorptionChiller(@dynamic.Name)
                {
                    DesignPressureDrop1 = absorptionChiller.DesignPressureDrop1,
                    DesignPressureDrop2 = absorptionChiller.DesignPressureDrop2,
                    DesignPressureDrop3 = absorptionChiller.DesignPressureDrop3,
                };
            }

            Modify.SetReference(systemChiller, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(absorptionChiller as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                systemChiller.SetValue(Core.Systems.SystemObjectParameter.AncillaryEnergySourceName, fuelSources[0].Name);
            }

            systemChiller.Description = dynamic.Description;
            systemChiller.Duty = dynamic.Duty?.ToSAM();

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

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
                result = new SystemAirSourceDirectAbsorptionChiller(@dynamic.Name);
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
                
                result = systemAirSourceChiller;
            }

            Modify.SetReference(result, @dynamic.GUID);

            List<FuelSource> fuelSources = Query.FuelSources(chiller as PlantComponent);
            if (fuelSources != null && fuelSources.Count > 0)
            {
                result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0].Name);
                if (fuelSources.Count > 1)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[1].Name);
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
                result = new SystemWaterSourceDirectAbsorptionChiller(@dynamic.Name);
            }
            else
            {
                SystemWaterSourceChiller systemWaterSourceChiller = new SystemWaterSourceChiller(@dynamic.Name);
                systemWaterSourceChiller.Setpoint = @dynamic.Setpoint?.ToSAM();
                systemWaterSourceChiller.Efficiency = @dynamic.Efficiency?.ToSAM();
                systemWaterSourceChiller.Capacity1 = @dynamic.Capacity1;
                systemWaterSourceChiller.DesignPressureDrop1 = @dynamic.DesignPressureDrop1;
                systemWaterSourceChiller.DesignTemperatureDifference1 = @dynamic.DesignDeltaT1;
                systemWaterSourceChiller.Capacity2 = @dynamic.Capacity2;
                systemWaterSourceChiller.DesignPressureDrop2 = @dynamic.DesignPressureDrop2;
                systemWaterSourceChiller.DesignTemperatureDifference2 = @dynamic.DesignDeltaT2;

                result = systemWaterSourceChiller;
            }

            result.Description = dynamic.Description;
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
                result = new SystemWaterSourceIceStorageChiller(@dynamic.Name);
                if (fuelSources != null && fuelSources.Count > 0)
                {
                    result.SetValue(Core.Systems.SystemObjectParameter.EnergySourceName, fuelSources[0]?.Name);
                    if (fuelSources.Count > 1)
                    {
                        result.SetValue(Core.Systems.SystemObjectParameter.FanEnergySourceName, fuelSources[1]?.Name);
                    }
                }
            }
            else
            {
                result = new SystemIceStorageChiller(@dynamic.Name);
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
            }

            result.Description = dynamic.Description;
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
