using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SizedFlowValue ToSAM(this SizedFlowVariable sizedFlowVariable)
        {
            if (sizedFlowVariable == null)
            {
                return null;
            }

            //TODO: To be implemented

            return new SizedFlowValue(sizedFlowVariable.Value, sizedFlowVariable.SizeFraction);
        }
    }
}
