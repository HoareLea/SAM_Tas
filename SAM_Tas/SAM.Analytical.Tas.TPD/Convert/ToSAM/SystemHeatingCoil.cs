using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatingCoil ToSAM(this global::TPD.HeatingCoil heatingCoil)
        {
            if (heatingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = heatingCoil;

            SystemHeatingCoil result = new SystemHeatingCoil(dynamic.Name);
            result.Description = dynamic.Description;

            result.Setpoint = heatingCoil.Setpoint?.ToSAM();
            result.Efficiency = heatingCoil.Efficiency?.ToSAM();
            result.Duty = heatingCoil.Duty?.ToSAM(); 
            result.MaximumOffcoil = heatingCoil.MaximumOffcoil?.ToSAM();
            result.ControlBand = heatingCoil.SetpointControlBand;

            Modify.SetReference(result, @dynamic.GUID);

            result.ScheduleName = ((dynamic)heatingCoil )?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)heatingCoil);
            if (collectionLink != null)
            {
                result.SetValue(SystemHeatingColiParameter.HeatingCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemHeatingCoil displaySystemHeatingCoil = Systems.Create.DisplayObject<DisplaySystemHeatingCoil>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemHeatingCoil != null)
            {
                ITransform2D transform2D = ((ISystemComponent)heatingCoil).Transform2D();
                if (transform2D != null)
                {
                    displaySystemHeatingCoil.Transform(transform2D);
                }

                result = displaySystemHeatingCoil;
            }

            return result;
        }
    }
}
