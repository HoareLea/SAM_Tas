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

        public static List<TIC.InternalCondition> InternalConditions(this TIC.Document document)
        {
            if(document == null)
            {
                return null;
            }

            TIC.InternalConditionFolder internalConditionFolder = document.internalConditionRoot;
            if(internalConditionFolder == null)
            {
                return null;
            }


            return internalConditionFolder.InternalConditions();
        }

        public static List<TIC.InternalCondition> InternalConditions(this TIC.InternalConditionFolder internalConditionFolder)
        {
            if(internalConditionFolder == null)
            {
                return null;
            }

            List<TIC.InternalCondition> result = new List<TIC.InternalCondition>();

            int index;

            index = 1;
            TIC.InternalCondition internalCondition = internalConditionFolder.internalConditions(index);
            while(internalCondition != null)
            {
                result.Add(internalCondition);
                index++;
                internalCondition = internalConditionFolder.internalConditions(index);
            }

            index = 1;
            TIC.InternalConditionFolder internalConditionFolder_Child = internalConditionFolder.childFolders(index);
            while(internalConditionFolder_Child != null)
            {
                List<TIC.InternalCondition> internalConditions = internalConditionFolder_Child?.InternalConditions();
                if(internalConditions != null)
                {
                    result.AddRange(internalConditions);
                }
                index++;
                internalConditionFolder_Child = internalConditionFolder.childFolders(index);
            }

            return result;
        }
    }
}