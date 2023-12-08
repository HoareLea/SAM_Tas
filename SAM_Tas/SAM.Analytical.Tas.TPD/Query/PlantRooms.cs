using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantRoom> PlantRooms(this TPDDoc tPDDoc)
        {
            return PlantRooms(tPDDoc?.EnergyCentre);
        }

        public static List<PlantRoom> PlantRooms(this EnergyCentre energyCentre)
        {
            if (energyCentre == null)
            {
                return null;
            }

            int index = 1;

            List<PlantRoom> result = new List<PlantRoom>();

            PlantRoom plantRoom = energyCentre.GetPlantRoom(index);
            while (plantRoom != null)
            {
                result.Add(plantRoom);
                index++;
                plantRoom = energyCentre.GetPlantRoom(index);
            }

            return result;
        }
    }
}