using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, bool[]> Compare(Dictionary<string, double[]> values_TBD, Dictionary<string, double[]> values_TSD, double margin, LoadType loadType)
        {
            if (values_TBD == null || values_TSD is null)
                return null;

            Dictionary<string, bool[]> result = new Dictionary<string, bool[]>();
            foreach (string reference in values_TBD.Keys)
            {
                if (!values_TBD.TryGetValue(reference, out double[] values_TBD_Reference) || !values_TSD.TryGetValue(reference, out double[] values_TSD_Reference))
                {
                    result.Add(reference, null);
                    continue;
                }

                bool[] values = new bool[8760];

                result.Add(reference, values);
                if (loadType == LoadType.Undefined)
                    continue;

                for (int i = 0; i < 8760; i++)
                {
                    if (i >= values_TBD_Reference.Length || i >= values_TSD_Reference.Length)
                    {
                        values[i] = false;
                        continue;
                    }

                    double aValue_TBD = values_TBD_Reference[i];
                    double aValue_TSD = values_TSD_Reference[i];

                    if (loadType == LoadType.Heating)
                        values[i] = aValue_TSD + margin >= aValue_TBD;
                    else
                        values[i] = aValue_TSD - margin <= aValue_TBD;
                }

            }

            return result;
        }
    }
}