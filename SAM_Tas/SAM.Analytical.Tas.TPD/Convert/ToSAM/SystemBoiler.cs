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

            SystemBoiler systemBoiler = new SystemBoiler(@dynamic.Name);
            systemBoiler.Description = dynamic.Description;
            Modify.SetReference(systemBoiler, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemBoiler result = Systems.Create.DisplayObject<DisplaySystemBoiler>(systemBoiler, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)bolierPlan).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
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

            SystemBoiler systemBoiler = new SystemBoiler(@dynamic.Name);
            systemBoiler.Description = dynamic.Description;
            Modify.SetReference(systemBoiler, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemBoiler result = Systems.Create.DisplayObject<DisplaySystemBoiler>(systemBoiler, location, Systems.Query.DefaultDisplaySystemManager());

            ITransform2D transform2D = ((IPlantComponent)multiBoiler).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
