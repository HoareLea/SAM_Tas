using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.DaysShade UpdateDaysShade(this TBD.Building building, int dayIndex)
        {
            if(building == null)
            {
                return null;
            }

            if(dayIndex < 0)
            {
                return null;
            }

            List<TBD.DaysShade> daysShades = building.DaysShades();

            TBD.DaysShade result = daysShades.Find(x => x.day == dayIndex);
            if(result == null)
            {
                result = building.AddDaysShade();
                result.day = dayIndex;
            }

            return result;
        }
    }
}