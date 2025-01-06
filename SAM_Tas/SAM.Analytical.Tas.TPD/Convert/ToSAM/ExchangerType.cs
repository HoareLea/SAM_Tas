using TPD;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ExchangerType ToSAM(this tpdExchangerType tpdExchangerType)
        {
            switch(tpdExchangerType)
            {
                case tpdExchangerType.tpdExchangerSimple:
                    return ExchangerType.Simple;

                case tpdExchangerType.tpdExchangerCounterFlow:
                    return ExchangerType.CounterFlow;

                case tpdExchangerType.tpdExchangerCrossFlowMixed:
                    return ExchangerType.CrossFlowMixed;

                case tpdExchangerType.tpdExchangerCrossFlowUnmixed:
                    return ExchangerType.CrossFlowUnmixed;

                case tpdExchangerType.tpdExchangerParallelFlow:
                    return ExchangerType.ParallelFlow;

            }

            return ExchangerType.Simple;
        }
    }
}
