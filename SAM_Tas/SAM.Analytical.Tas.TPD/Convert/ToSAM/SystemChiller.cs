using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Systems;

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

            bool waterSource = absorptionChiller.IsWaterSource == 1;

            dynamic @dynamic = absorptionChiller;

            SystemChiller systemChiller = null;
            if (waterSource)
            {
                systemChiller = new SystemWaterSourceAbsorptionChiller(@dynamic.Name);
            }
            else
            {
                systemChiller = new SystemAbsorptionChiller(@dynamic.Name);
            }

            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

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
                result = new SystemAirSourceChiller(@dynamic.Name);
            }

            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

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
                result = new SystemWaterSourceChiller(@dynamic.Name);
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

            bool waterSource = iceStorageChiller.IsWaterSource == 1;

            dynamic @dynamic = iceStorageChiller;

            SystemChiller result = null;
            if (waterSource)
            {
                result = new SystemWaterSourceIceStorageChiller(@dynamic.Name);
            }
            else
            {
                result = new SystemIceStorageChiller(@dynamic.Name);
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
