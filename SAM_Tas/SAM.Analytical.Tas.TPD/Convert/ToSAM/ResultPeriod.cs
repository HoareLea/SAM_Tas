using SAM.Core.Tas.TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static ResultPeriod ToSAM(this global::TPD.tpdResultsPeriod tpdResultsPeriod)
        {
            switch (tpdResultsPeriod)
            {
                case global::TPD.tpdResultsPeriod.tpdResultsPeriodHourly:
                    return ResultPeriod.Hourly;

                case global::TPD.tpdResultsPeriod.tpdResultsPeriodDaily:
                    return ResultPeriod.Daily;

                case global::TPD.tpdResultsPeriod.tpdResultsPeriodWeekly:
                    return ResultPeriod.Weekly;

                case global::TPD.tpdResultsPeriod.tpdResultsPeriodMonthly:
                    return ResultPeriod.Monthly;

                case global::TPD.tpdResultsPeriod.tpdResultsPeriodAnnual:
                    return ResultPeriod.Annual;

            }

            return ResultPeriod.Undefined;
        }
    }
}
