using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> RemoveUnusedInternalConditions(this TBD.Building building)
        {
            if (building == null)
            {
                return null;
            }

            List<TBD.zone> zones = building.Zones();
            if(zones == null)
            {
                return null;
            }

            HashSet<string> names = new HashSet<string>();
            foreach(TBD.zone zone in zones)
            {
                List<TBD.InternalCondition> internalConditions = zone?.InternalConditions();
                if(internalConditions == null)
                {
                    continue;
                }

                foreach(TBD.InternalCondition internalCondition_Temp in internalConditions)
                {
                    string name = internalCondition_Temp?.name;
                    if(string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    names.Add(name);
                }
            }

            List<string> result = new List<string>();

            List<int> indexes = new List<int>();

            int index = 0;
            TBD.InternalCondition internalCondition = building.GetIC(index);
            while (internalCondition != null)
            {
                string name = internalCondition?.name;
                if(!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                {
                    indexes.Add(index);
                }

                index++;
                internalCondition = building.GetIC(index);
            }

            for (int i = indexes.Count - 1; i >= 0; i--)
            {
                result.Add(building.GetIC(indexes[i]).name);
                building.RemoveIC(indexes[i]);
            }

            return result;
        }
    }
}