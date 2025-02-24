using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static CurveModifierType ToSAM(this tpdProfileDataCurveType tpdProfileDataCurveType)
        {
            switch(tpdProfileDataCurveType)
            {
                case tpdProfileDataCurveType.tpdProfileDataCurveLinear:
                    return CurveModifierType.Linear;

                case tpdProfileDataCurveType.tpdProfileDataCurveBiLinear:
                    return CurveModifierType.BiLinear;

                case tpdProfileDataCurveType.tpdProfileDataCurveTriLinear:
                    return CurveModifierType.TriLinear;

                case tpdProfileDataCurveType.tpdProfileDataCurveQuadratic:
                    return CurveModifierType.Quadratic;

                case tpdProfileDataCurveType.tpdProfileDataCurveBiQuadratic:
                    return CurveModifierType.BiQuadratic;

                case tpdProfileDataCurveType.tpdProfileDataCurveTriQuadratic:
                    return CurveModifierType.TriQuadratic;

                case tpdProfileDataCurveType.tpdProfileDataCurveCubic:
                    return CurveModifierType.Cubic;

                case tpdProfileDataCurveType.tpdProfileDataCurveBiCubic:
                    return CurveModifierType.BiCubic;
            }

            throw new System.NotImplementedException();
        }
    }
}
