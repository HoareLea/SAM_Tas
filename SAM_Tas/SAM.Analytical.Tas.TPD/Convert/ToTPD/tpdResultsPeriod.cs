using SAM.Core.Tas.TPD;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdResultsPeriod ToTPD(this ResultPeriod resultPeriod)
        {
            switch (resultPeriod)
            {
                case ResultPeriod.Hourly:
                    return tpdResultsPeriod.tpdResultsPeriodHourly;

                case ResultPeriod.Daily:
                    return tpdResultsPeriod.tpdResultsPeriodDaily;

                case ResultPeriod.Weekly:
                    return tpdResultsPeriod.tpdResultsPeriodWeekly;

                case ResultPeriod.Monthly:
                    return tpdResultsPeriod.tpdResultsPeriodMonthly;

                case ResultPeriod.Annual:
                    return tpdResultsPeriod.tpdResultsPeriodAnnual;
            }

            throw new System.NotImplementedException();
        }
    }
}
