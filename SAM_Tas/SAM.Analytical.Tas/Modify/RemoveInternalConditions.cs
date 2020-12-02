using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<bool> RemoveInternalConditions(this TBD.Building building, IEnumerable<string> names)
        {
            if (building == null || names == null)
                return null;

            HashSet<string> hashSet = new HashSet<string>();
            foreach (string name in names)
                hashSet.Add(name);

            List<int> indexes = new List<int>();

            List<bool> result = new List<bool>();

            int index = 0;
            TBD.InternalCondition internalCondition = building.GetIC(index);
            while (internalCondition != null)
            {
                string name = internalCondition.name;

                bool contains = hashSet.Contains(name);
                result.Add(contains);

                if (contains)
                    indexes.Add(index);

                index++;
                internalCondition = building.GetIC(index);
            }

            for(int i = indexes.Count - 1; i >= 0; i--)
                building.RemoveIC(indexes[i]);

            return result;
        }
    }
}