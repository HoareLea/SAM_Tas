using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemHeatingCoil ToSAM(this HeatingCoil heatingCoil)
        {
            if (heatingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = heatingCoil;

            SystemHeatingCoil systemHeatingCoil = new SystemHeatingCoil(dynamic.Name);
            systemHeatingCoil.Description = dynamic.Description;
            Modify.SetReference(systemHeatingCoil, @dynamic.GUID);

            Point2D location =((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemHeatingCoil result = Systems.Create.DisplayObject<DisplaySystemHeatingCoil>(systemHeatingCoil, location, Systems.Query.DefaultDisplaySystemManager());

            result.Duty = heatingCoil.Duty.ToSAM_Duty();

            ITransform2D transform2D = ((ISystemComponent)heatingCoil).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
