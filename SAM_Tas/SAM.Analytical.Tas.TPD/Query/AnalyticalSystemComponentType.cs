using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static AnalyticalSystemComponentType AnalyticalSystemComponentType(this ISystemComponent systemComponent)
        { 
            if(systemComponent == null)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
            }

            if(systemComponent is CoolingCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemCoolingCoil;
            }

            if (systemComponent is HeatingCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemHeatingCoil;
            }

            return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
        }
    }
}