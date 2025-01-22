using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdLoadDistribution ToTPD(this LoadDistribution loadDistribution)
        {
            switch (loadDistribution)
            {
                case LoadDistribution.Daily:
                    return tpdLoadDistribution.tpdLoadDistributionDaily;

                case LoadDistribution.None:
                    return tpdLoadDistribution.tpdLoadDistributionNone;

                case LoadDistribution.Even:
                    return tpdLoadDistribution.tpdLoadDistributionEven;
            }

            throw new System.NotImplementedException();
        }
    }
}
