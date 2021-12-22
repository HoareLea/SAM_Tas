using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<Tuple<string, string, string>> DesignDayNames(this string path_TSD)
        {
            if (string.IsNullOrWhiteSpace(path_TSD))
            {
                return null;
            }

            List<Tuple<string, string, string>> result = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD))
            {
                Dictionary<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> dictionary = DesignDataDictionary(sAMTSDDocument);
                if(dictionary != null)
                {
                    result = new List<Tuple<string, string, string>>();
                    foreach(KeyValuePair<string, Tuple<CoolingDesignData, double, int, HeatingDesignData, double, int>> keyValuePair in dictionary)
                    {
                        result.Add(new Tuple<string, string, string>(keyValuePair.Key, keyValuePair.Value.Item1?.name, keyValuePair.Value.Item4?.name));
                    }
                }

            }

            return result;
        }
    }
}