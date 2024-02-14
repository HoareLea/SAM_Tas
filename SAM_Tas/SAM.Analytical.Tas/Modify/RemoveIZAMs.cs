using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool RemoveIZAMs(this TBD.Building building)
        {
            if (building == null)
                return false;

            TBD.IZAM iZAM = building.GetIZAM(0);
            while (iZAM != null)
            {
                building.RemoveIZAM(0);
                iZAM = building.GetIZAM(0);
            }

            return true;
        }

        public static List<bool> RemoveIZAMs(this TBD.Building building, IEnumerable<string> names)
        {
            if (building == null || names == null)
                return null;

            HashSet<string> hashSet = new HashSet<string>();
            foreach (string name in names)
                hashSet.Add(name);

            List<int> indexes = new List<int>();

            List<bool> result = new List<bool>();

            int index = 0;
            TBD.IZAM iZAM = building.GetIZAM(index);
            while (iZAM != null)
            {
                string name = iZAM.name;

                bool contains = hashSet.Contains(name);
                result.Add(contains);

                if (contains)
                    indexes.Add(index);

                index++;
                iZAM = building.GetIZAM(index);
            }

            for (int i = indexes.Count - 1; i >= 0; i--)
            {
                building.RemoveIZAM(indexes[i]);
            }

            return result;
        }
    }
}