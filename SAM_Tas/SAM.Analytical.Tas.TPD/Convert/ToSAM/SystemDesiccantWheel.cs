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

            SystemDesiccantWheel result = new SystemDesiccantWheel(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            result = Systems.Create.DisplayObject<DisplaySystemDesiccantWheel>(result, location, Systems.Query.DefaultDisplaySystemManager());

            return result;
        }
    }
}
