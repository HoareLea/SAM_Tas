using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemCoolingCoil ToSAM(this CoolingCoil coolingCoil)
        {
            if (coolingCoil == null)
            {
                return null;
            }

            dynamic @dynamic = coolingCoil;

            SystemCoolingCoil systemCoolingCoil = new SystemCoolingCoil(@dynamic.Name);
            systemCoolingCoil.Description = dynamic.Description;
            Modify.SetReference(systemCoolingCoil, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemCoolingCoil result = Systems.Create.DisplayObject<DisplaySystemCoolingCoil>(systemCoolingCoil, location, Systems.Query.DefaultDisplaySystemManager());
            result.BypassFactor = coolingCoil.BypassFactor.Value;
            switch(coolingCoil.Duty.Type)
            {
                case tpdSizedVariable.tpdSizedVariableSize:
                    result.Duty = new SizedDuty(double.NaN, coolingCoil.Duty.SizeFraction);
                    break;

                case tpdSizedVariable.tpdSizedVariableNone:
                    result.Duty = new UnlimitedDuty();
                    break;


                case tpdSizedVariable.tpdSizedVariableValue:
                    result.Duty = new Duty(double.NaN);
                    break;
            }


            ITransform2D transform2D = ((ISystemComponent)coolingCoil).Transform2D();
            if (transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
