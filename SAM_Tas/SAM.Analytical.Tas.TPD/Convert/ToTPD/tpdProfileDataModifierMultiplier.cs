using SAM.Core;
using SAM.Core.Tas.TPD;
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
            }

            throw new System.NotImplementedException();
        }
    }
}
