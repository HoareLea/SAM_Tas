using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static Dictionary<string, IndexedDoubles> Dictionary<T>(this Dictionary<T, IndexedDoubles> dictionary) where T: Enum
        { 
            if(dictionary == null)
            {
                return null;
            }

            Dictionary<string, IndexedDoubles> result = new Dictionary<string, IndexedDoubles>();
            foreach (KeyValuePair<T, IndexedDoubles> keyValuePair in dictionary)
            {
                result[keyValuePair.Key.ToString()] = keyValuePair.Value;
            }

            return result;
        }

        public static Dictionary<string, IndexedDoubles> Dictionary(this Enum @enum, IndexedDoubles indexedDoubles)
        {
            if (indexedDoubles == null || @enum == null)
            {
                return null;
            }

            Dictionary<string, IndexedDoubles> result = new Dictionary<string, IndexedDoubles>();
            result[@enum.ToString()] = indexedDoubles;

            return result;
        }
    }
}