using System.Collections.Generic;
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

            return false;

        }

        public static bool SetSchedule(this ZoneComponent zoneComponent, string scheduleName)
        {
            if (zoneComponent == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = Query.Schedules(((dynamic)zoneComponent)?.GetZone()?.GetSystem()?.GetPlantRoom()?.GetEnergyCentre() as EnergyCentre);
            if (plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach (PlantSchedule plantSchedule in plantSchedules)
            {
                if (plantSchedule?.Name == scheduleName)
                {
                    ((dynamic)zoneComponent).SetSchedule(plantSchedule);
                    return true;
                }
            }

            return false;

        }

        public static bool SetSchedule(this FuelSource fuelSource, string scheduleName)
        {
            if (fuelSource == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = Query.Schedules(((dynamic)fuelSource).GetEnergyCentre() as EnergyCentre);
            if (plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach (PlantSchedule plantSchedule in plantSchedules)
            {
                if (plantSchedule?.Name == scheduleName)
                {
                    fuelSource.Schedule = plantSchedule;
                    return true;
                }
            }

            return false; ;

        }

        public static bool SetSchedule(this Controller controller, string scheduleName)
        {
            if (controller == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = Query.Schedules(((dynamic)controller).GetSystem().GetPlantRoom()?.GetEnergyCentre() as EnergyCentre);
            if (plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach (PlantSchedule plantSchedule in plantSchedules)
            {
                if (plantSchedule?.Name == scheduleName)
                {
                    ((dynamic)controller).SetSchedule(plantSchedule);
                    return true;
                }
            }

            return false; ;

        }

        public static bool SetSchedule(this PlantController plantController, string scheduleName)
        {
            if (plantController == null || scheduleName == null)
            {
                return false;
            }

            List<PlantSchedule> plantSchedules = Query.Schedules(((dynamic)plantController).GetPlantRoom()?.GetEnergyCentre() as EnergyCentre);
            if (plantSchedules == null || plantSchedules.Count == 0)
            {
                return false;
            }

            foreach (PlantSchedule plantSchedule in plantSchedules)
            {
                if (plantSchedule?.Name == scheduleName)
                {
                    ((dynamic)plantController).SetSchedule(plantSchedule);
                    return true;
                }
            }

            return false; ;

        }
    }
}