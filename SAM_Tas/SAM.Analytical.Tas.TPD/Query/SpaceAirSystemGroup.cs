using SAM.Analytical.Systems;
using SAM.Core.Systems;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static bool TryGetSystemSpace(this SystemPlantRoom systemPlantRoom, ISystemComponent systemComponent, out ISystemSpace systemSpace, out AirSystemGroup airSystemGroup)
        {
            airSystemGroup = null;
            systemSpace = null;

            if (systemComponent == null || systemPlantRoom == null)
            {
                return false;
            }

            List<AirSystemGroup> airSystemGroups = systemPlantRoom.GetRelatedObjects<AirSystemGroup>(systemComponent);
            if (airSystemGroups == null || airSystemGroups.Count == 0)
            {
                return false;
            }

            foreach (AirSystemGroup airSystemGroup_Temp in airSystemGroups)
            {
                List<ISystemSpace> systemSpaces = systemPlantRoom.GetRelatedObjects<ISystemSpace>(airSystemGroup_Temp);
                if (systemSpaces != null && systemSpaces.Count != 0)
                {
                    airSystemGroup = airSystemGroup_Temp;
                    systemSpace = systemSpaces[0];
                    return true;
                }
            }

            return false;
        }
    }
}