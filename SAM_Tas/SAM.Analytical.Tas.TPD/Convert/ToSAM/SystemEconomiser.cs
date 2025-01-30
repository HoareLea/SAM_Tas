using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemEconomiser ToSAM_SystemEconomiser(this Optimiser optimizer)
        {
            if (optimizer == null)
            {
                return null;
            }

            dynamic @dynamic = optimizer;

            SystemEconomiser result = new SystemEconomiser(@dynamic.Name);
            result.Description = dynamic.Description;

            result.Capacity = optimizer.Capacity;
            result.DesignFlowRate = optimizer.DesignFlowRate?.ToSAM();
            result.DesignFlowType = optimizer.DesignFlowType.ToSAM();
            result.Setpoint = optimizer.Setpoint?.ToSAM();
            result.MinFreshAirRate = optimizer.MinFreshAirRate?.ToSAM();
            result.MinFreshAirType = optimizer.MinFreshAirType.ToSAM();
            result.ScheduleMode = optimizer.ScheduleMode.ToSAM();
            result.DesignPressureDrop = optimizer.DesignPressureDrop;

            result.ScheduleName = ((dynamic)optimizer )?.GetSchedule()?.Name;

            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemEconomiser displaySystemEconomiser = Systems.Create.DisplayObject<DisplaySystemEconomiser>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemEconomiser != null)
            {
                ITransform2D transform2D = ((ISystemComponent)optimizer).Transform2D();
                if (transform2D != null)
                {
                    displaySystemEconomiser.Transform(transform2D);
                }

                result = displaySystemEconomiser;
            }

            return result;
        }
    }
}
