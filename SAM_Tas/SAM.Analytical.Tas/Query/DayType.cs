namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TBD.dayType DayType(this TBD.TBDDocument tBDDocument, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            TBD.Calendar calendar = tBDDocument?.Building?.GetCalendar();
            if (calendar == null)
                return null;

            int index = 1;
            TBD.dayType dayType = calendar.dayTypes(index);
            while (dayType != null)
            {
                if (dayType.name.Equals(name))
                    return dayType;

                index++;
                dayType = calendar.dayTypes(index);
            }

            return null;
        }
    }
}