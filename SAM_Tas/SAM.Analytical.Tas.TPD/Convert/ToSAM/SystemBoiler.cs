using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemBoiler ToSAM(this BoilerPlant bolierPlan)
        {
            if (bolierPlan == null)
            {
                return null;
            }

            dynamic @dynamic = bolierPlan;

            SystemBoiler result = new SystemBoiler(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);
            
            result.Description = dynamic.Description;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemBoiler displaySystemBoiler = Systems.Create.DisplayObject<DisplaySystemBoiler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemBoiler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)bolierPlan).Transform2D();
                if (transform2D != null)
                {
                    displaySystemBoiler.Transform(transform2D);
                }

                result = displaySystemBoiler;
            }

            return result;
        }

        public static SystemBoiler ToSAM(this MultiBoiler multiBoiler)
        {
            if (multiBoiler == null)
            {
                return null;
            }

            dynamic @dynamic = multiBoiler;

            SystemBoiler result = new SystemBoiler(@dynamic.Name);
            result.Description = dynamic.Description;
            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemBoiler displaySystemBoiler = Systems.Create.DisplayObject<DisplaySystemBoiler>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemBoiler != null)
            {
                ITransform2D transform2D = ((IPlantComponent)multiBoiler).Transform2D();
                if (transform2D != null)
                {
                    displaySystemBoiler.Transform(transform2D);
                }

                result = displaySystemBoiler;
            }

            return result;
        }
    }
}
