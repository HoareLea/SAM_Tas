using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMixingBox ToSAM_SystemMixingBox(this Optimiser optimizer)
        {
            if (optimizer == null)
            {
                return null;
            }

            dynamic @dynamic = optimizer;

            SystemMixingBox systemMixingBox = new SystemMixingBox(@dynamic.Name);
            systemMixingBox.Description = dynamic.Description;
            Modify.SetReference(systemMixingBox, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMixingBox result = Systems.Create.DisplayObject<DisplaySystemMixingBox>(systemMixingBox, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)systemMixingBox).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
