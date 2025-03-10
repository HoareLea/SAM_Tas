using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static List<IZoneComponent> AddSystemZoneComponents(this SystemZone systemZone, SystemSpace systemSpace, SystemPlantRoom systemPlantRoom)
        {
            if(systemSpace == null || systemSpace == null || systemPlantRoom == null)
            {
                return null;
            }

            return Add(systemZone, systemPlantRoom.GetSystemSpaceComponents<ISystemSpaceComponent>(systemSpace));
        }

    }
}