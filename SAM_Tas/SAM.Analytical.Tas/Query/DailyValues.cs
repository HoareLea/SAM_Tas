namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static double[] DailyValues(this TBD.profile profile)
        {
            if (profile == null)
                return null;
            
            double[] result = new double[24];

            for (int i = 1; i <= 24; i++)
                result[i - 1] = profile.hourlyValues[i];

            return result;
        }
    }
}