using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantRoom> PlantRooms(this TPDDoc tPDDoc)
        { 
            if(tPDDoc == null)
            {
                return null;
            }

            int index = 1;

            List<PlantRoom> result = new List<PlantRoom>();

            PlantRoom plantRoom = tPDDoc.EnergyCentre.GetPlantRoom(index);
            while(plantRoom != null)
            {
                result.Add(plantRoom);
                index++;
                plantRoom = tPDDoc.EnergyCentre.GetPlantRoom(index);
            }

            return result;
        }
    }
}