using TPD;
using System.Linq;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemSpace ToSAM(this SystemZone systemZone)
        {
            if (systemZone == null)
            {
                return null;
            }

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if (zoneLoad == null)
            {
                return null;
            }

            dynamic @dynamic = systemZone as dynamic;

            double flowRate = double.NaN;
            if(systemZone.FlowRate.Type != tpdSizedVariable.tpdSizedVariableNone)
            {
                flowRate = systemZone.FlowRate.Value;
            }

            double freshAirRate = double.NaN;
            if (systemZone.FreshAir.Type != tpdSizedVariable.tpdSizedVariableNone)
            {
                freshAirRate = systemZone.FreshAir.Value;
            }

            SystemSpace result = new SystemSpace(zoneLoad.Name, zoneLoad.FloorArea, zoneLoad.Volume, flowRate, freshAirRate);
            result.Description = dynamic.Description;
            Modify.SetReference(result, dynamic.GUID);

            return result;
        }
    }
}
