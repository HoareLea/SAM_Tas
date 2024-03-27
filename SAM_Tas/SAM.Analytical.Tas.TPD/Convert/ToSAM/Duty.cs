using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Duty ToSAM_Duty(this SizedVariable sizedVariable)
        {
            if(sizedVariable == null)
            {
                return null;
            }

            dynamic @dynamic = sizedVariable as dynamic;

            switch ((tpdSizedVariable)@dynamic.Type)
            {
                case tpdSizedVariable.tpdSizedVariableSizeDone:
                    return new SizedDuty(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction);

                case tpdSizedVariable.tpdSizedVariableSize:
                    return new SizedDuty(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction);

                case tpdSizedVariable.tpdSizedVariableNone:
                    return new UnlimitedDuty();

                case tpdSizedVariable.tpdSizedVariableValue:
                    return new Duty(System.Convert.ToDouble(@dynamic.Value));
            }

            return null;
        }
    }
}
