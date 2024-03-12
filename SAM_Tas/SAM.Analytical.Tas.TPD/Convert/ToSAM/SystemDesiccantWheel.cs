using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDesiccantWheel ToSAM(this DesiccantWheel desiccantWheel)
        {
            if (desiccantWheel == null)
            {
                return null;
            }

            dynamic @dynamic = desiccantWheel;

            SystemDesiccantWheel systemDesiccantWheel = new SystemDesiccantWheel(@dynamic.Name);
            systemDesiccantWheel.Description = dynamic.Description;
            Modify.SetReference(systemDesiccantWheel, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDesiccantWheel result = Systems.Create.DisplayObject<DisplaySystemDesiccantWheel>(systemDesiccantWheel, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)desiccantWheel).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
