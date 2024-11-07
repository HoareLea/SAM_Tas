using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantController> PlantControllers(this PlantRoom plantRoom)
        {
            if (plantRoom == null)
            {
                return null;
            }

            List<PlantController> result = new List<PlantController>();

            int count = plantRoom.GetControllerCount();
            for (int i = 1; i <= count; i++)
            {
                PlantController plantController = plantRoom.GetController(i);
                if (plantController != null)
                {
                    result.Add(plantController);
                }
            }

            return result;
        }

        public static List<PlantController> PlantControllers(this IPlantComponentGroup plantComponentGroup)
        {
            if (plantComponentGroup == null)
            {
                return null;
            }

            List<PlantController> result = new List<PlantController>();

            int count = plantComponentGroup.GetControllerCount();
            for (int i = 1; i <= count; i++)
            {
                PlantController plantController = plantComponentGroup.GetController(i);
                if (plantController != null)
                {
                    result.Add(plantController);
                }
            }

            return result;
        }
    }
}