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

            Optimiser result = system.AddOptimiser();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemEconomiser.Name;
            @dynamic.Description = displaySystemEconomiser.Description;

            result.Capacity = displaySystemEconomiser.Capacity;
            result.DesignFlowRate?.Update(displaySystemEconomiser.DesignFlowRate);
            result.DesignFlowType = displaySystemEconomiser.DesignFlowType.ToTPD();
            result.Setpoint?.Update(displaySystemEconomiser.Setpoint);
            result.MinFreshAirRate?.Update(displaySystemEconomiser.MinFreshAirRate);
            result.MinFreshAirType = displaySystemEconomiser.MinFreshAirType.ToTPD();
            result.ScheduleMode = displaySystemEconomiser.ScheduleMode.ToTPD();
            result.DesignPressureDrop = displaySystemEconomiser.DesignPressureDrop;

            //result.ScheduleMode = tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;
            //result.MinFreshAirType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;
            //result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            result.Flags = 0;

            if (plantSchedule != null)
            {
                @dynamic.SetSchedule(plantSchedule);
            }

            displaySystemEconomiser.SetLocation(result as SystemComponent);

            return result;
        }

        public static Optimiser ToTPD(this DisplaySystemMixingBox displaySystemMixingBox, global::TPD.System system, PlantSchedule plantSchedule)
        {
            if (displaySystemMixingBox == null || system == null)
            {
                return null;
            }

            Optimiser result = system.AddOptimiser();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemMixingBox.Name;
            @dynamic.Description = displaySystemMixingBox.Description;

            result.Capacity = displaySystemMixingBox.Capacity;
            result.DesignFlowRate?.Update(displaySystemMixingBox.DesignFlowRate);
            result.DesignFlowType = displaySystemMixingBox.DesignFlowType.ToTPD();
            result.Setpoint?.Update(displaySystemMixingBox.Setpoint);
            result.MinFreshAirRate?.Update(displaySystemMixingBox.MinFreshAirRate);
            result.MinFreshAirType = displaySystemMixingBox.MinFreshAirType.ToTPD();
            result.ScheduleMode = displaySystemMixingBox.ScheduleMode.ToTPD();
            result.DesignPressureDrop = displaySystemMixingBox.DesignPressureDrop;

            //result.ScheduleMode = tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;
            //result.MinFreshAirType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;
            //result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            result.Flags = (int)tpdOptimiserFlags.tpdOptimiserFlagFreshAirMixingBox;
            if (plantSchedule != null)
            {
                @dynamic.SetSchedule(plantSchedule);
            }

            displaySystemMixingBox.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
