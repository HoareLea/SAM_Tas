using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Damper ToTPD(this DisplaySystemDamper displaySystemDamper, global::TPD.System system, Damper damper = null)
        {
            if(displaySystemDamper == null || system == null)
            {
                return null;
            }

            Damper result = damper;
            if(result == null)
            {
                result = system.AddDamper();
            }

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDamper.Name;
            @dynamic.Description = displaySystemDamper.Description;

            EnergyCentre energyCentre = system.GetPlantRoom()?.GetEnergyCentre();

            result.Capacity = displaySystemDamper.Capacity;
            result.DesignCapacitySignal = displaySystemDamper.DesignCapacitySignal;
            result.DesignFlowRate?.Update(displaySystemDamper.DesignFlowRate, energyCentre);
            result.DesignFlowType = displaySystemDamper.DesignFlowType.ToTPD();
            result.MinimumFlowRate?.Update(displaySystemDamper.MinimumFlowRate, energyCentre);
            result.MinimumFlowType = displaySystemDamper.MinimumFlowType.ToTPD();
            result.MinimumFlowFraction = displaySystemDamper.MinimumFlowFraction;
            result.DesignPressureDrop = displaySystemDamper.DesignPressureDrop;

            Modify.SetSchedule((SystemComponent)result, displaySystemDamper.ScheduleName);

            if(damper == null)
            {
                displaySystemDamper.SetLocation(result as SystemComponent);
            }

            return result;
        }
    }
}
