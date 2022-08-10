using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool CopyFrom(this TBD.Calendar tBDCalendar, TCR.Calendar tCRCalendar)
        {
            if (tBDCalendar == null || tCRCalendar == null)
                return false;

            tBDCalendar.name = tCRCalendar.name;
            tBDCalendar.description = tCRCalendar.description;
            tBDCalendar.startDay = tCRCalendar.startDay;

            tBDCalendar.RemoveDayTypes();
            List<TCR.DayType> dayTypes =  tCRCalendar.DayTypes();
            foreach(TCR.DayType dayType in dayTypes)
            {
                TBD.dayType dayType_TBD = tBDCalendar.AddDayType();
                dayType_TBD.color = dayType.color;
                dayType_TBD.description = dayType.description;
                dayType_TBD.endBreak = dayType.endBreak ? 1 : 0;
                dayType_TBD.name = dayType.description;
            }

            for (int i = 1; i <= 365; i++)
            {
                TBD.day day_TBD = tBDCalendar.days(i);
                TCR.Day day_TCR = tCRCalendar.days(i);

                day_TBD.dayType = tBDCalendar.DayType(day_TCR.DayType.description);
            }
            return true;

        }

        public static bool CopyFrom(this TBD.Calendar tBDCalendar, TCR.Document tCRDocument, string name, Core.TextComparisonType textComparisonType = Core.TextComparisonType.Equals, bool caseSensitive = true)
        {
            return CopyFrom(tBDCalendar, tCRDocument?.Calendar(name, textComparisonType, caseSensitive));
        }
  }
}