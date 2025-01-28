using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Damper ToTPD(this DisplaySystemDamper displaySystemDamper, global::TPD.System system)
        {
            if(displaySystemDamper == null || system == null)
            {
                return null;
            }

            Damper result = system.AddDamper();

            dynamic @dynamic = result;
            @dynamic.Name = displaySystemDamper.Name;
            @dynamic.Description = displaySystemDamper.Description;

            result.Capacity = displaySystemDamper.Capacity;
            result.DesignCapacitySignal = displaySystemDamper.DesignCapacitySignal;
            result.DesignFlowRate?.Update(displaySystemDamper.DesignFlowRate);
            result.DesignFlowType = displaySystemDamper.DesignFlowType.ToTPD();
            result.MinimumFlowRate?.Update(displaySystemDamper.MinimumFlowRate);
            result.MinimumFlowType = displaySystemDamper.MinimumFlowType.ToTPD();
            result.MinimumFlowFraction = displaySystemDamper.MinimumFlowFraction;
            result.DesignPressureDrop = displaySystemDamper.DesignPressureDrop;


            displaySystemDamper.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
