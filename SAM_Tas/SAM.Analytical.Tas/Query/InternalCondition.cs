namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TBD.InternalCondition InternalCondition(this TBD.zone zone, TBD.dayType dayType)
        {
            if (zone == null)
                return null;

            int index = 0;
            TBD.InternalCondition internalCondition = zone.GetIC(index);
            while (internalCondition != null)
            {
                if (HasDayType(internalCondition, dayType))
                    return internalCondition;

                index++;
                internalCondition = zone.GetIC(index);
            }

            return null;
        }
    }
}