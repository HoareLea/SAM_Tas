using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Optimiser ToTPD(this DisplaySystemEconomiser displaySystemEconomiser, global::TPD.System system, PlantSchedule plantSchedule)
        {
            if(displaySystemEconomiser == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddOptimiser();
            result.SetSchedule(plantSchedule);
            result.ScheduleMode = tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;
            result.MinFreshAirType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;
            result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            displaySystemEconomiser.SetLocation(result as SystemComponent);

            return result as Optimiser;
        }
    }
}
