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

            bool directAbsorptionChiller = chiller.IsDirectAbsChiller == 1;
            
            dynamic @dynamic = chiller;

            SystemChiller systemChiller = null;
            if (directAbsorptionChiller)
            {
                systemChiller = new SystemAirSourceDirectAbsorptionChiller(@dynamic.Name);
            }
            else
            {
                systemChiller = new SystemAirSourceChiller(@dynamic.Name);
            }

            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject result = null;
            if (directAbsorptionChiller)
            {
                result = Systems.Create.DisplayObject<DisplaySystemAirSourceDirectAbsorptionChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                result = Systems.Create.DisplayObject<DisplaySystemAirSourceChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }

            ITransform2D transform2D = ((IPlantComponent)chiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result as SystemChiller;
        }

        public static SystemChiller ToSAM(this WaterSourceChiller waterSourceChiller)
        {
            if (waterSourceChiller == null)
            {
                return null;
            }

            bool directAbsorptionChiller = waterSourceChiller.IsDirectAbsChiller == 1;

            dynamic @dynamic = waterSourceChiller;

            SystemChiller systemChiller = null;
            if (directAbsorptionChiller)
            {
                systemChiller = new SystemWaterSourceDirectAbsorptionChiller(@dynamic.Name);
            }
            else
            {
                systemChiller = new SystemWaterSourceChiller(@dynamic.Name);
            }

            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject result = null;
            if (directAbsorptionChiller)
            {
                result = Systems.Create.DisplayObject<DisplaySystemWaterSourceDirectAbsorptionChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                result = Systems.Create.DisplayObject<DisplaySystemWaterSourceChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }

            ITransform2D transform2D = ((IPlantComponent)waterSourceChiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result as SystemChiller;
        }

        public static SystemChiller ToSAM(this IceStorageChiller iceStorageChiller)
        {
            if (iceStorageChiller == null)
            {
                return null;
            }

            bool waterSource = iceStorageChiller.IsWaterSource == 1;

            dynamic @dynamic = iceStorageChiller;

            SystemChiller systemChiller = null;
            if (waterSource)
            {
                systemChiller = new SystemWaterSourceIceStorageChiller(@dynamic.Name);
            }
            else
            {
                systemChiller = new SystemIceStorageChiller(@dynamic.Name);
            }

            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            IDisplaySystemObject result = null;
            if (waterSource)
            {
                result = Systems.Create.DisplayObject<DisplaySystemWaterSourceIceStorageChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }
            else
            {
                result = Systems.Create.DisplayObject<DisplaySystemIceStorageChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());
            }

            ITransform2D transform2D = ((IPlantComponent)iceStorageChiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result as SystemChiller;
        }
    }
}
