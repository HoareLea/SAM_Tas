using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TCR.Calendar> Calendars(this TCR.Document document)
        {
            return Calendars(document.calendarRoot);
        }

        public static List<TCR.Calendar> Calendars(this TCR.CalendarFolder calendarFolder)
        {
            if (calendarFolder == null)
            {
                return null;
            }

            List<TCR.Calendar> result = new List<TCR.Calendar>();

            int index;

            index = 1;
            TCR.Calendar calendar = calendarFolder.calendars(index);
            while (calendar != null)
            {
                result.Add(calendar);
                index++;
                calendar = calendarFolder.calendars(index);
            }

            index = 1;
            TCR.CalendarFolder calendarFolder_Child = calendarFolder.childFolders(index);
            while (calendarFolder_Child != null)
            {
                List<TCR.Calendar> calendars = calendarFolder_Child.Calendars();
                if (calendars != null)
                {
                    result.AddRange(calendars);
                }
                index++;
                calendarFolder_Child = calendarFolder.childFolders(index);
            }

            return result;
        }
    }
}