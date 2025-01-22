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

            result.Capacity = displaySystemDamper.Capacity;
            result.DesignCapacitySignal = displaySystemDamper.DesignCapacitySignal;
            result.MinimumFlowFraction = displaySystemDamper.MinimumFlowFraction;
            result.DesignPressureDrop = displaySystemDamper.DesignPressureDrop;


            displaySystemDamper.SetLocation(result as SystemComponent);

            return result;
        }
    }
}
