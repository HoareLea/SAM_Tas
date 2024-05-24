using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoil ToSAM(this DXCoil dxCoil)
        {
            if (dxCoil == null)
            {
                return null;
            }

            dynamic @dynamic = dxCoil;

            SystemDXCoil systemDXCoil = new SystemDXCoil(dynamic.Name);
            systemDXCoil.Description = dynamic.Description;
            Modify.SetReference(systemDXCoil, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDXCoil result = Systems.Create.DisplayObject<DisplaySystemDXCoil>(systemDXCoil, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)dxCoil).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
