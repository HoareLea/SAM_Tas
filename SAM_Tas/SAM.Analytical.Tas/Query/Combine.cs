using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static bool[] Combine(IEnumerable<bool[]> values)
        {
            if (values == null)
            {
                return null;
            }

            int count = 0;
            foreach (bool[] bools in values)
            {
                if (bools.Length > count)
                {
                    count = bools.Length;
                }
            }

            bool[] result = new bool[count];

            for(int i =0; i < count; i++)
            {
                result[i] = true;
                foreach(bool[] bools in values)
                {
                    if (bools.Length <= i)
                    {
                        continue;
                    }

                    if (!bools[i])
                    {
                        result[i] = false;
                        break;
                    }
                }
            }

            return result;
        }
    }
}