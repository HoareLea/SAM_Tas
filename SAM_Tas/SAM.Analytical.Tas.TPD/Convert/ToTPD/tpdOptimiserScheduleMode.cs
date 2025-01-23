using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static tpdOptimiserScheduleMode ToTPD(this ScheduleMode scheduleMode)
        {
            switch (scheduleMode)
            {
                case ScheduleMode.Recirc:
                    return tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;

                case ScheduleMode.NoMinimum:
                    return tpdOptimiserScheduleMode.tpdOptimiserScheduleNoMinimum;

                case ScheduleMode.Through:
                    return tpdOptimiserScheduleMode.tpdOptimiserScheduleThrough;

            }

            throw new System.NotImplementedException();
        }
    }
}
