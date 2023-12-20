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

            SystemSpace result = new SystemSpace(zoneLoad.Name, zoneLoad.FloorArea, zoneLoad.Volume);

            Modify.SetReference(result, (systemZone as dynamic).GUID);

            return result;
        }
    }
}
