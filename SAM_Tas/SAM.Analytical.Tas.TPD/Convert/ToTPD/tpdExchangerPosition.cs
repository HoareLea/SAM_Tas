using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdExchangerPosition ToTPD(this ExchangerPosition exchangerPosition)
        {
            switch (exchangerPosition)
            {
                case ExchangerPosition.Left:
                    return tpdExchangerPosition.tpdExchangerPositionLeft;

                case ExchangerPosition.RightOverLeft:
                    return tpdExchangerPosition.tpdExchangerPositionRightOverLeft;

                case ExchangerPosition.LeftOverRight:
                    return tpdExchangerPosition.tpdExchangerPositionLeftOverRight;

                case ExchangerPosition.Right:
                    return tpdExchangerPosition.tpdExchangerPositionRight;

                case ExchangerPosition.None:
                    return tpdExchangerPosition.tpdExchangerPositionNone;

            }

            throw new System.NotImplementedException();
        }
    }
}
