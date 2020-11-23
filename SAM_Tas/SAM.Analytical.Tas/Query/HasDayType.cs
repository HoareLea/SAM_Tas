namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static bool HasDayType(TBD.InternalCondition internalCondition, TBD.dayType dayType)
        {
            int index = 0;
            TBD.dayType aDayType = internalCondition.GetDayType(index);
            while (aDayType != null)
            {
                if (aDayType.name == dayType.name)
                    return true;

                index++;
                aDayType = internalCondition.GetDayType(index);
            }

            return false;
        }
    }
}