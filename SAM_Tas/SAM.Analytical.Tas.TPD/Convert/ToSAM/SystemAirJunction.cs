using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemAirJunction ToSAM(this Junction junction)
        {
            if (junction == null)
            {
                return null;
            }

            dynamic @dynamic = junction;

            SystemAirJunction systemAirJunction = new SystemAirJunction(@dynamic.Name);
            systemAirJunction.Description = dynamic.Description;
            Modify.SetReference(systemAirJunction, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemAirJunction result = Systems.Create.DisplayObject<DisplaySystemAirJunction>(systemAirJunction, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)junction).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
