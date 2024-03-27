using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDamper ToSAM(this Damper damper)
        {
            if (damper == null)
            {
                return null;
            }

            dynamic @dynamic = damper;

            SystemDamper systemDamper = new SystemDamper(@dynamic.Name);
            systemDamper.Description = dynamic.Description;
            Modify.SetReference(systemDamper, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDamper result = Systems.Create.DisplayObject<DisplaySystemDamper>(systemDamper, location, Systems.Query.DefaultDisplaySystemManager());
            if(result == null)
            {
                return null;
            }

            ITransform2D transform2D = ((ISystemComponent)damper).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
