using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemLiquidJunction ToSAM(this PlantJunction plantJunction)
        {
            if (plantJunction == null)
            {
                return null;
            }

            dynamic @dynamic = plantJunction;

            SystemLiquidJunction systemLiquidJunction = new SystemLiquidJunction(@dynamic.Name);
            systemLiquidJunction.Description = dynamic.Description;
            Modify.SetReference(systemLiquidJunction, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemLiquidJunction result = Systems.Create.DisplayObject<DisplaySystemLiquidJunction>(systemLiquidJunction, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((ISystemComponent)plantJunction).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}

