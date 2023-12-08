using TPD;
using System.Linq;

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
            result.SetReference(zoneLoad.GUID);

            return result;
        }
    }
}
