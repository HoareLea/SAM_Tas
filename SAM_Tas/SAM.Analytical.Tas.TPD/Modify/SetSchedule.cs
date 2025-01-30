﻿using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        public static bool SetSchedule(this SystemComponent systemComponent, string scheduleName)
        { 
            if(systemComponent == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = Query.Schedules(((dynamic)systemComponent).GetSystem().GetPlantRoom()?.GetEnergyCentre() as EnergyCentre);
            if(plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach(PlantSchedule plantSchedule in plantSchedules)
            {
                if(plantSchedule?.Name == scheduleName)
                {
                    ((dynamic)systemComponent).SetSchedule(plantSchedule); ;
                    return true;
                }
            }

            return false; ;

        }

        public static bool SetSchedule(this PlantComponent plantComponent, string scheduleName)
        {
            if (plantComponent == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = Query.Schedules(((dynamic)plantComponent).GetPlantRoom()?.GetEnergyCentre() as EnergyCentre);
            if (plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach (PlantSchedule plantSchedule in plantSchedules)
            {
                if (plantSchedule?.Name == scheduleName)
                {
                    ((dynamic)plantComponent).SetSchedule(plantSchedule);
                    return true;
                }
            }

            return false; ;

        }
    }
}