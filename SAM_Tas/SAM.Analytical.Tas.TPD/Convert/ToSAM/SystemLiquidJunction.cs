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

            SystemLiquidJunction result = new SystemLiquidJunction(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            result.MainsPressure = plantJunction.MainsPressure;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemLiquidJunction displaySystemLiquidJunction = Systems.Create.DisplayObject<DisplaySystemLiquidJunction>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemLiquidJunction != null)
            {
                ITransform2D transform2D = ((IPlantComponent)plantJunction).Transform2D();
                if (transform2D != null)
                {
                    displaySystemLiquidJunction.Transform(transform2D);
                }

                result = displaySystemLiquidJunction;
            }

            return result;
        }
    }
}

