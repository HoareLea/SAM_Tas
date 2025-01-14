using TPD;
using System;
using System.Collections.Generic;
using SAM.Core.Systems;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static List<Controller> Controllers(this SystemPlantRoom systemPlantRoom, global::TPD.System system, AirSystem airSystem, Dictionary<Guid, global::TPD.ISystemComponent> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return null;
            }

            List<Controller> result = new List<Controller>();

            List<IDisplaySystemController> displaySystemControllers = systemPlantRoom.GetSystemComponents<IDisplaySystemController>(airSystem);
            foreach(IDisplaySystemController displaySystemController in displaySystemControllers)
            {
                Controller plantController = displaySystemController.ToTPD(system);
                if(plantController == null)
                {
                    continue;
                }

                result.Add(plantController);
            }

            return result;
        }

    }
}
