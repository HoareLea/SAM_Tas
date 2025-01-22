using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdExchangerType ToTPD(this ExchangerType exchangerType)
        {
            switch (exchangerType)
            {
                case ExchangerType.CounterFlow:
                    return tpdExchangerType.tpdExchangerCounterFlow;

                case ExchangerType.CrossFlowUnmixed:
                    return tpdExchangerType.tpdExchangerCrossFlowUnmixed;

                case ExchangerType.CrossFlowMixed:
                    return tpdExchangerType.tpdExchangerCrossFlowMixed;

                case ExchangerType.ParallelFlow:
                    return tpdExchangerType.tpdExchangerParallelFlow;

                case ExchangerType.Simple:
                    return tpdExchangerType.tpdExchangerSimple;
            }

            throw new System.NotImplementedException();
        }
    }
}
