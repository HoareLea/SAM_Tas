using SAM.Analytical.Systems;
using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdProfileDataVariableType ToTPD(this CurveModifierVariableType curveModifierVariableType)
        {

            foreach(tpdProfileDataVariableType tpdProfileDataVariableType in Enum.GetValues(typeof(tpdProfileDataVariableType)))
            {
                if(tpdProfileDataVariableType.ToSAM() == curveModifierVariableType)
                {
                    return tpdProfileDataVariableType;
                }
            }

            throw new NotImplementedException();
        }
    }
}
