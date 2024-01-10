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

            List<global::TPD.System> result = new List<global::TPD.System>();

            int count = plantRoom.GetSystemCount();
            for (int i = 1; i <= count; i++)
            {
                global::TPD.System system = plantRoom.GetSystem(i);
                result.Add(system);
            }

            return result;
        }
    }
}