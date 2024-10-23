using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemChiller ToSAM(this Chiller chiller)
        {
            if (chiller == null)
            {
                return null;
            }

            dynamic @dynamic = chiller;

            SystemChiller systemChiller = new SystemChiller(@dynamic.Name);
            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemChiller result = Systems.Create.DisplayObject<DisplaySystemChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)chiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }

        public static SystemChiller ToSAM(this MultiChiller multiChiller)
        {
            if (multiChiller == null)
            {
                return null;
            }

            dynamic @dynamic = multiChiller;

            SystemChiller systemChiller = new SystemChiller(@dynamic.Name);
            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemChiller result = Systems.Create.DisplayObject<DisplaySystemChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)multiChiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
