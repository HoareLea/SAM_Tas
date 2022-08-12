namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool RemoveDayTypes(this TBD.Calendar calendar)
        {
            if (calendar == null)
                return false;

            bool result = false;
            while (calendar.GetDayTypeCount() != 0)
            {
                calendar.RemoveDayType(1);
                result = true;
            }

            return result;
        }
    }
}