using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SizableValue ToSAM(this SizedVariable sizedVariable)
        {
            if(sizedVariable == null)
            {
                return null;
            }

            dynamic @dynamic = sizedVariable as dynamic;

            switch ((tpdSizedVariable)@dynamic.Type)
            {
                case tpdSizedVariable.tpdSizedVariableSizeDone:
                    return new SizedValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction);

                case tpdSizedVariable.tpdSizedVariableSize:
                    return new SizedValue(System.Convert.ToDouble(@dynamic.Value), @dynamic.SizeFraction);

                case tpdSizedVariable.tpdSizedVariableNone:
                    return new UnlimitedValue();

                case tpdSizedVariable.tpdSizedVariableValue:
                    return new SizableValue(System.Convert.ToDouble(@dynamic.Value));
            }

            return null;
        }
    }
}
