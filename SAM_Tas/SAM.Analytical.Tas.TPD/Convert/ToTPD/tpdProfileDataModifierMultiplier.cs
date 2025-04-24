using SAM.Core;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdProfileDataModifierMultiplier ToTPD(this ArithmeticOperator arithmeticOperator)
        {
            switch (arithmeticOperator)
            {
                case ArithmeticOperator.Addition:
                    return tpdProfileDataModifierMultiplier.tpdProfileDataModifierAdd;

                case ArithmeticOperator.Division:
                    return tpdProfileDataModifierMultiplier.tpdProfileDataModifierDivide;

                case ArithmeticOperator.Multiplication:
                    return tpdProfileDataModifierMultiplier.tpdProfileDataModifierMultiply;

                case ArithmeticOperator.Modulus:
                    return tpdProfileDataModifierMultiplier.tpdProfileDataModifierEqual;
            }

            throw new System.NotImplementedException();
        }
    }
}
