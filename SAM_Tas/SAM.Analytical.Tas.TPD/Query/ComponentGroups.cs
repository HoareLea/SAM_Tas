using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<ComponentGroup> ComponentGroups(this EnergyCentre energyCentre)
        {
            List<PlantRoom> plantRooms = energyCentre?.PlantRooms();
            if(plantRooms == null)
            {
                return null;
            }

            List<ComponentGroup> result = new List<ComponentGroup>();

            foreach (PlantRoom plantRoom in plantRooms)
            {
                List<global::TPD.System> systems = plantRoom.Systems();
                if (systems == null || systems.Count == 0)
                {
                    continue;
                }

                foreach (global::TPD.System system in systems)
                {
                    List<SystemComponent> systemComponents = system.SystemComponents<SystemComponent>();
                    if (systemComponents == null)
                    {
                        continue;
                    }

                    foreach (SystemComponent systemComponent in systemComponents)
                    {
                        ComponentGroup componentGroup = systemComponent as ComponentGroup;
                        if (componentGroup == null)
                        {
                            continue;
                        }

                        result.Add(componentGroup);
                    }
                }
            }

            return result;
        }
    }
}