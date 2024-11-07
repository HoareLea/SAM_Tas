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

            SystemDamper result = new SystemDamper(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDamper displaySystemDamper = Systems.Create.DisplayObject<DisplaySystemDamper>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemDamper != null)
            {
                ITransform2D transform2D = ((ISystemComponent)damper).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDamper.Transform(transform2D);
                }

                result = displaySystemDamper;
            }

            return result;
        }
    }
}
