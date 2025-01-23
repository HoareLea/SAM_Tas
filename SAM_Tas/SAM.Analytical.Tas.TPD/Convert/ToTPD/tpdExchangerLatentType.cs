using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdExchangerLatentType ToTPD(this ExchangerLatentType exchangerLatentType)
        {
            switch (exchangerLatentType)
            {
                case ExchangerLatentType.Enthalpy:
                    return tpdExchangerLatentType.tpdExchangerLatentEnthalpy;

                case ExchangerLatentType.HumidityRatio:
                    return tpdExchangerLatentType.tpdExchangerLatentHumRat;

            }

            throw new System.NotImplementedException();
        }
    }
}
