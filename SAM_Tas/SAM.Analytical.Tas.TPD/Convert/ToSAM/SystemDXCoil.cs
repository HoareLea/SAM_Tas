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

            SystemDXCoil systemDXCoil = new SystemDXCoil(dynamic.Name);
            systemDXCoil.Description = dynamic.Description;
            Modify.SetReference(systemDXCoil, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemDXCoil result = Systems.Create.DisplayObject<DisplaySystemDXCoil>(systemDXCoil, location, Systems.Query.DefaultDisplaySystemManager());
            result.CoolingSetpoint = dxCoil.CoolingSetpoint.ToSAM();
            result.HeatingSetpoint = dxCoil.HeatingSetpoint.ToSAM();
            result.MinOffcoilTemperature = dxCoil.MinimumOffcoil.ToSAM();
            result.MaxOffcoilTemperature = dxCoil.MaximumOffcoil.ToSAM();
            result.BypassFactor = dxCoil.BypassFactor.ToSAM();
            result.CoolingDuty = dxCoil.CoolingDuty.ToSAM();
            result.HeatingDuty = dxCoil.HeatingDuty.ToSAM();

            ITransform2D transform2D = ((ISystemComponent)dxCoil).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
