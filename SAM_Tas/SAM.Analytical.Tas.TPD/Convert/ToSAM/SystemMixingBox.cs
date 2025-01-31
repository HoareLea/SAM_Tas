using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemMixingBox ToSAM_SystemMixingBox(this Optimiser optimizer)
        {
            if (optimizer == null)
            {
                return null;
            }

            dynamic @dynamic = optimizer;

            SystemMixingBox result = new SystemMixingBox(@dynamic.Name);
            result.Description = dynamic.Description;

            result.ScheduleName = ((dynamic)optimizer )?.GetSchedule()?.Name;

            Modify.SetReference(result, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemMixingBox displaySystemMixingBox = Systems.Create.DisplayObject<DisplaySystemMixingBox>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemMixingBox != null)
            {
                ITransform2D transform2D = ((ISystemComponent)optimizer).Transform2D();
                if (transform2D != null)
                {
                    displaySystemMixingBox.Transform(transform2D);
                }

                result = displaySystemMixingBox;
            }

            return result;
        }
    }
}
