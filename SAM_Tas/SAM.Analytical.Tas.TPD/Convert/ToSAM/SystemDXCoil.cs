using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemDXCoil ToSAM(this DXCoil dxCoil)
        {
            if (dxCoil == null)
            {
                return null;
            }

            dynamic @dynamic = dxCoil;

            SystemDXCoil result = new SystemDXCoil(dynamic.Name);
            Modify.SetReference(result, @dynamic.GUID);

            result.Description = dynamic.Description;
            result.CoolingSetpoint = dxCoil.CoolingSetpoint.ToSAM();
            result.HeatingSetpoint = dxCoil.HeatingSetpoint.ToSAM();
            result.MinOffcoilTemperature = dxCoil.MinimumOffcoil.ToSAM();
            result.MaxOffcoilTemperature = dxCoil.MaximumOffcoil.ToSAM();
            result.BypassFactor = dxCoil.BypassFactor.ToSAM();
            result.CoolingDuty = dxCoil.CoolingDuty.ToSAM();
            result.HeatingDuty = dxCoil.HeatingDuty.ToSAM();

            result.ScheduleName = ((dynamic)dxCoil )?.GetSchedule()?.Name;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDXCoil displaySystemDXCoil = Systems.Create.DisplayObject<DisplaySystemDXCoil>(result, location, Systems.Query.DefaultDisplaySystemManager());
            if(displaySystemDXCoil != null)
            {
                ITransform2D transform2D = ((ISystemComponent)dxCoil).Transform2D();
                if (transform2D != null)
                {
                    displaySystemDXCoil.Transform(transform2D);
                }

                result = displaySystemDXCoil;
            }

            RefrigerantGroup refrigerantGroup = @dynamic.GetRefrigerantGroup();
            if (refrigerantGroup != null)
            {
                result.SetValue(SystemDXColiParameter.RefrigerantCollection, new CollectionLink(CollectionType.Refrigerant, ((dynamic)refrigerantGroup).Name));
            }

            return result;
        }
    }
}
