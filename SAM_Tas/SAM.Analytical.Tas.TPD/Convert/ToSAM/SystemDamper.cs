using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDamper ToSAM(this Damper damper)
        {
            if (damper == null)
            {
                return null;
            }

            dynamic @dynamic = damper;

            SystemDamper result = new SystemDamper(@dynamic.Name);
            result.Description = dynamic.Description;
            result.Capacity = damper.Capacity;
            result.DesignCapacitySignal = damper.DesignCapacitySignal;
            result.DesignFlowRate = damper.DesignFlowRate?.ToSAM();
            result.DesignFlowType = damper.DesignFlowType.ToSAM();
            result.MinimumFlowRate = damper.MinimumFlowRate?.ToSAM();
            result.MinimumFlowType = damper.MinimumFlowType.ToSAM();
            result.MinimumFlowFraction = damper.MinimumFlowFraction;
            result.DesignPressureDrop = damper.DesignPressureDrop;

            Modify.SetReference(result, @dynamic.GUID);

            result.ScheduleName = ((dynamic)damper )?.GetSchedule()?.Name;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDamper displaySystemDamper = Systems.Create.DisplayObject<DisplaySystemDamper>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemDamper != null)
            {
                ITransform2D transform2D = ((ISystemComponent)damper).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDamper.Transform(transform2D);
                }

                result = displaySystemDamper;
            }

            return result;
        }
    }
}
