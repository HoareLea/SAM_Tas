using SAM.Analytical.Systems;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static AnalyticalSystemComponentType AnalyticalSystemComponentType(this global::TPD.ISystemComponent systemComponent)
        { 
            if(systemComponent == null)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
            }

            if(systemComponent is global::TPD.CoolingCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemCoolingCoil;
            }

            if (systemComponent is global::TPD.HeatingCoil)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemHeatingCoil;
            }

            if (systemComponent is Junction)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemAirJunction;
            }

            if (systemComponent is global::TPD.Fan)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemFan;
            }

            if (systemComponent is DesiccantWheel)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemDesiccantWheel;
            }

            if (systemComponent is Exchanger)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemExchanger;
            }

            if (systemComponent is SystemZone)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemSpace;
            }

            if (systemComponent is Damper)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemDamper;
            }

            return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
        }

        public static AnalyticalSystemComponentType AnalyticalSystemComponentType(this IPlantComponent plantComponent)
        {
            if (plantComponent is Pump)
            {
                return Analytical.Systems.AnalyticalSystemComponentType.SystemPump;
            }

            return Analytical.Systems.AnalyticalSystemComponentType.Undefined;
        }
    }
}