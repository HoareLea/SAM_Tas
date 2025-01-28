using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<PlantSchedule> Schedules(this EnergyCentre energyCentre) 
        { 
            if(energyCentre == null)
            {
                return null;
            }

            List<PlantSchedule> result = new List<PlantSchedule>();

            int count = energyCentre.GetScheduleCount();
            for (int i = 1; i <= count; i++)
            {
                PlantSchedule plantSchedule = energyCentre.GetSchedule(i);
                if (plantSchedule == null)
                {
                    continue;
                }

                result.Add(plantSchedule);
            }

            return result;
        }

        public static List<PlantSchedule> Schedules(this global::TPD.System system)
        {
            return Schedules(system?.GetPlantRoom()?.GetEnergyCentre());
        }
    }
}