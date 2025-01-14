using TPD;
using System;
using System.Collections.Generic;
using SAM.Core.Systems;
using SAM.Analytical.Systems;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        public static List<PlantController> PlantControllers(this SystemPlantRoom systemPlantRoom, PlantRoom plantRoom, LiquidSystem liquidSystem, Dictionary<Guid, PlantComponent> dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return null;
            }

            List<PlantController> result = new List<PlantController>();

            List<IDisplaySystemController> displaySystemControllers = systemPlantRoom.GetSystemComponents<IDisplaySystemController>(liquidSystem);
            foreach(IDisplaySystemController displaySystemController in displaySystemControllers)
            {
                PlantController plantController = displaySystemController.ToTPD(plantRoom);
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
