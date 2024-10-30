using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMultiChiller ToSAM(this MultiChiller multiChiller)
        {
            if (multiChiller == null)
            {
                return null;
            }

            dynamic @dynamic = multiChiller;

            SystemMultiChiller systemChiller = new SystemMultiChiller(@dynamic.Name);
            systemChiller.Description = dynamic.Description;
            Modify.SetReference(systemChiller, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMultiChiller result = Systems.Create.DisplayObject<DisplaySystemMultiChiller>(systemChiller, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)multiChiller).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
