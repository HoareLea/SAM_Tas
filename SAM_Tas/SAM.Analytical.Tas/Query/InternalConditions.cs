using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.InternalCondition> InternalConditions(this TBD.TBDDocument tBDDocument)
        {
            return InternalConditions(tBDDocument?.Building);
        }

        public static List<TBD.InternalCondition> InternalConditions(this TBD.Building building)
        {
            if (building == null)
                return null;

            List<TBD.InternalCondition> result = new List<TBD.InternalCondition>();

            int count = 0;
            TBD.InternalCondition internalCondition = building.GetIC(count);
            while (internalCondition != null)
            {
                result.Add(internalCondition);

                count++;
                internalCondition = building.GetIC(count);
            }

            return result;
        }

        public static List<TBD.InternalCondition> InternalConditions(this TBD.zone zone)
        {
            if (zone == null)
                return null;

            List<TBD.InternalCondition> result = new List<TBD.InternalCondition>();

            int count = 0;
            TBD.InternalCondition internalCondition = zone.GetIC(count);
            while (internalCondition != null)
            {
                result.Add(internalCondition);

                count++;
                internalCondition = zone.GetIC(count);
            }

            return result;
        }
    }
}