using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Linq;

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

            SystemValve result = new SystemValve(@dynamic.Name)
            {
                Description = dynamic.Description,
                Capacity = dynamic.Capacity,
                DesignCapacitySignal = dynamic.DesignCapacitySignal,
                DesignFlowRate = dynamic.DesignFlowRate,
                DesignPressureDrop = dynamic.DesignPressureDrop
            };

            Modify.SetReference(result, @dynamic.GUID);

            if (result.DesignFlowRate == 0)
            {
                Pipe pipe = Query.Pipes((PlantComponent)valve, Core.Direction.Out)?.FirstOrDefault();
                if (pipe != null)
                {
                    result.DesignFlowRate = pipe.DesignFlowRate;
                }
            }

            result.ScheduleName = ((dynamic)valve )?.GetSchedule()?.Name;

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