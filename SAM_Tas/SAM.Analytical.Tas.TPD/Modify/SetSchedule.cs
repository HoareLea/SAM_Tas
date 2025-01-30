using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool SetSchedule(this global::TPD.SystemComponent systemComponent, string scheduleName)
        { 
            if(systemComponent == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = systemComponent.GetSystem()?.Schedules();
            if(plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach(PlantSchedule plantSchedule in plantSchedules)
            {
                if(plantSchedule?.Name == scheduleName)
                {
                    systemComponent.SetSchedule(plantSchedule);
                    return true;
                }
            }

            return false; ;

        }
    }
}