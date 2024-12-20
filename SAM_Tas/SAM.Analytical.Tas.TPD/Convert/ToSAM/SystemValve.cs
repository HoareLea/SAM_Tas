using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemValve ToSAM(this Valve valve)
        {
            if (valve == null)
            {
                return null;
            }

            dynamic @dynamic = valve;

            SystemValve result = new SystemValve(@dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.Capacity = dynamic.Capacity;
            result.DesignCapacitySignal = dynamic.DesignCapacitySignal;
            result.DesignFlowRate = dynamic.DesignFlowRate;
            result.DesignPressureDrop = dynamic.DesignPressureDrop;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemValve displaySystemValve = Systems.Create.DisplayObject<DisplaySystemValve>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if (displaySystemValve != null)
            {
                ITransform2D transform2D = ((IPlantComponent)valve).Transform2D();
                if (transform2D != null)
                {
                    displaySystemValve.Transform(transform2D);
                }

                result = displaySystemValve;
            }

            return result;
        }
    }
}