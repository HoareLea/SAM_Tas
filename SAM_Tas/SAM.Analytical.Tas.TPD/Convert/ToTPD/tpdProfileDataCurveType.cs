using SAM.Analytical.Systems;
using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdProfileDataCurveType ToTPD(this CurveModifierType curveModifierType)
        {

            foreach(tpdProfileDataCurveType tpdProfileDataCurveType in Enum.GetValues(typeof(tpdProfileDataCurveType)))
            {
                if(tpdProfileDataCurveType.ToSAM() == curveModifierType)
                {
                    return tpdProfileDataCurveType;
                }
            }

            throw new NotImplementedException();
        }
    }
}
