using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.schedule> Schedules(this TBD.Building building)
        {
            List<TBD.schedule> result = new List<TBD.schedule>();

            int index = 0;
            TBD.schedule schedule = building.GetSchedule(index);
            while (schedule != null)
            {
                result.Add(schedule);
                index++;

                schedule = building.GetSchedule(index);
            }

            return result;
        }
    }
}