using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingCoil ToSAM(this global::TPD.CoolingCoil coolingCoil)
        {
            if (coolingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = coolingCoil;

            SystemCoolingCoil result = new SystemCoolingCoil(@dynamic.Name);
            result.Description = dynamic.Description;

            result.Setpoint = coolingCoil.Setpoint?.ToSAM();
            result.BypassFactor = coolingCoil.BypassFactor?.ToSAM();
            result.Duty = coolingCoil.Duty?.ToSAM();
            result.MinimumOffcoil = coolingCoil.MinimumOffcoil?.ToSAM();

            Modify.SetReference(result, @dynamic.GUID);

            result.ScheduleName = ((dynamic)coolingCoil)?.GetSchedule()?.Name;

            CollectionLink collectionLink = Query.CollectionLink((ISystemComponent)coolingCoil);
            if(collectionLink != null)
            {
                result.SetValue(SystemCoolingCoilParameter.CoolingCollection, collectionLink);
            }

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCoolingCoil displaySystemCoolingCoil = Systems.Create.DisplayObject<DisplaySystemCoolingCoil>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemCoolingCoil != null)
            {
                ITransform2D transform2D = ((ISystemComponent)coolingCoil).Transform2D();
                if (transform2D != null)
                {
                    displaySystemCoolingCoil.Transform(transform2D);
                }

                result = displaySystemCoolingCoil;
            }

            return result;
        }
    }
}
