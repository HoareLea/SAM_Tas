using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ExchangerPosition ToSAM(this tpdExchangerPosition tpdExchangerPosition)
        {
            switch(tpdExchangerPosition)
            {
                case tpdExchangerPosition.tpdExchangerPositionNone:
                    return ExchangerPosition.None;

                case tpdExchangerPosition.tpdExchangerPositionLeft:
                    return ExchangerPosition.Left;

                case tpdExchangerPosition.tpdExchangerPositionLeftOverRight:
                    return ExchangerPosition.LeftOverRight;

                case tpdExchangerPosition.tpdExchangerPositionRight:
                    return ExchangerPosition.Right;

                case tpdExchangerPosition.tpdExchangerPositionRightOverLeft:
                    return ExchangerPosition.RightOverLeft;
            }

            return ExchangerPosition.None;
        }
    }
}
