using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.System> Systems(this PlantRoom plantRoom)
        { 
            if(plantRoom == null)
            {
                return null;
            }

            int index = 1;

            List<global::TPD.System> result = new List<global::TPD.System>();

            global::TPD.System system = plantRoom.GetSystem(index);
            while(system != null)
            {
                result.Add(system);
                index++;
                system = plantRoom.GetSystem(index);
            }

            return result;
        }
    }
}