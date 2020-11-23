using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<TBD.InternalCondition> InternalConditions(this TBD.TBDDocument tBDDocument)
        {
            TBD.Building building = tBDDocument?.Building;
            if (building == null)
                return null;

            List<TBD.InternalCondition> result = new List<TBD.InternalCondition>();

            int count = 0;
            TBD.InternalCondition internalCondition = building.GetIC(count);
            while (internalCondition != null)
            {
                result.Add(internalCondition);
                
                internalCondition = tBDDocument.Building.GetIC(count);
                count++;
            }

            return result;
        }
    }
}