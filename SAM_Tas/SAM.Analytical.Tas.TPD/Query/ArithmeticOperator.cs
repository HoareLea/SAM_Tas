using SAM.Core;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static ArithmeticOperator? ArithmeticOperator(this tpdProfileDataModifierMultiplier tpdProfileDataModifierMultiplier)
        {
            switch (tpdProfileDataModifierMultiplier)
            {
                case tpdProfileDataModifierMultiplier.tpdProfileDataModifierAdd:
                    return Core.ArithmeticOperator.Addition;

                case tpdProfileDataModifierMultiplier.tpdProfileDataModifierDivide:
                    return Core.ArithmeticOperator.Division;

                case tpdProfileDataModifierMultiplier.tpdProfileDataModifierMultiply:
                    return Core.ArithmeticOperator.Multiplication;

                case tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual:
                    return Core.ArithmeticOperator.Modulus;
            }

            return null;
        }
    }
}
