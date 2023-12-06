using TPD;
using System.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SpaceSystemCalculationResult SpaceSystemCalculationResult(this SystemZone systemZone, int start, int end)
        {
            if(systemZone == null)
            {
                return null;
            }

            ZoneLoad zoneLoad = systemZone.ZoneLoads()?.FirstOrDefault();
            if(zoneLoad == null)
            {
                return null;
            }

            List<FanCoilUnit> fanCoilUnits = systemZone.ZoneComponents<ZoneComponent>()?.FindAll(x => x is FanCoilUnit)?.ConvertAll(x => (FanCoilUnit)x);
            if(fanCoilUnits != null)
            {
                foreach(FanCoilUnit fanCoilUnit in fanCoilUnits)
                {
                    IndexedDoubles indexedDoubles = Create.IndexedDoubles((ZoneComponent)fanCoilUnit, ResultDataType.CoolingSensibleLoad, start, end);
                }
            }

            throw new System.NotImplementedException();
        }
    }
}
