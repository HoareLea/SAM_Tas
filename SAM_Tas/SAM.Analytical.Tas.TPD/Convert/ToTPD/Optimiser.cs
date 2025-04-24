using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Optimiser ToTPD(this DisplaySystemEconomiser displaySystemEconomiser, global::TPD.System system, Optimiser optimiser = null)
        {
            if(displaySystemEconomiser == null || system == null)
            {
                return null;
            }

            Optimiser result = optimiser;
            if(result == null)
            {
                result = system.AddOptimiser();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemEconomiser.Name;
            @dynamic.Description = displaySystemEconomiser.Description;

            EnergyCentre energyCentre = system.GetPlantRoom()?.GetEnergyCentre();

            result.Capacity = displaySystemEconomiser.Capacity;
            result.DesignFlowRate?.Update(displaySystemEconomiser.DesignFlowRate, energyCentre);
            result.DesignFlowType = displaySystemEconomiser.DesignFlowType.ToTPD();
            result.Setpoint?.Update(displaySystemEconomiser.Setpoint, energyCentre);
            result.MinFreshAirRate?.Update(displaySystemEconomiser.MinFreshAirRate, energyCentre);
            result.MinFreshAirType = displaySystemEconomiser.MinFreshAirType.ToTPD();
            result.ScheduleMode = displaySystemEconomiser.ScheduleMode.ToTPD();
            result.DesignPressureDrop = displaySystemEconomiser.DesignPressureDrop;

            //result.ScheduleMode = tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;
            //result.MinFreshAirType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;
            //result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            result.Flags = 0;

            Modify.SetSchedule((SystemComponent)result, displaySystemEconomiser.ScheduleName);

            if(optimiser == null)
            {
                displaySystemEconomiser.SetLocation((SystemComponent)result);
            }

            return result;
        }

        public static Optimiser ToTPD(this DisplaySystemMixingBox displaySystemMixingBox, global::TPD.System system, Optimiser optimiser = null)
        {
            if (displaySystemMixingBox == null || system == null)
            {
                return null;
            }

            Optimiser result = optimiser;
            if(result == null)
            {
                result = system.AddOptimiser();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemMixingBox.Name;
            @dynamic.Description = displaySystemMixingBox.Description;

            PlantRoom plantRoom = system.GetPlantRoom();

            EnergyCentre energyCentre = plantRoom.GetEnergyCentre();

            result.Capacity = displaySystemMixingBox.Capacity;
            result.DesignFlowRate?.Update(displaySystemMixingBox.DesignFlowRate, energyCentre);
            result.DesignFlowType = displaySystemMixingBox.DesignFlowType.ToTPD();
            result.Setpoint?.Update(displaySystemMixingBox.Setpoint, energyCentre);
            result.MinFreshAirRate?.Update(displaySystemMixingBox.MinFreshAirRate, energyCentre);
            result.MinFreshAirType = displaySystemMixingBox.MinFreshAirType.ToTPD();
            result.ScheduleMode = displaySystemMixingBox.ScheduleMode.ToTPD();
            result.DesignPressureDrop = displaySystemMixingBox.DesignPressureDrop;

            //result.ScheduleMode = tpdOptimiserScheduleMode.tpdOptimiserScheduleRecirc;
            //result.MinFreshAirType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFreshAir;
            //result.DesignFlowType = tpdFlowRateType.tpdFlowRateAllAttachedZonesFlowRate;

            result.Flags = (int)tpdOptimiserFlags.tpdOptimiserFlagFreshAirMixingBox;

            Modify.SetSchedule((SystemComponent)result, displaySystemMixingBox.ScheduleName);

            Modify.SetSchedule((SystemComponent)result, displaySystemMixingBox.ScheduleName);

            if(optimiser == null)
            {
                displaySystemMixingBox.SetLocation((SystemComponent)result);
            }

            return result;
        }
    }
}
