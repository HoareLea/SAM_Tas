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

            SystemAirJunction result = new SystemAirJunction(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemAirJunction displaySystemAirJunction = Systems.Create.DisplayObject<DisplaySystemAirJunction>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemAirJunction != null)
            {
                ITransform2D transform2D = ((ISystemComponent)junction).Transform2D();
                if (transform2D != null)
                {
                    displaySystemAirJunction.Transform(transform2D);
                }

                result = displaySystemAirJunction;
            }

            return result;
        }
    }
}