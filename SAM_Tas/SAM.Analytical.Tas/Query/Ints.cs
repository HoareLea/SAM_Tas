using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<int> Ints(this string @string, char separator = ',')
        {
            if (@string == null)
                return null;

            List<int> result = new List<int>();
            if (string.IsNullOrWhiteSpace(@string))
                return null;

            foreach(string string_value in @string.Split(separator))
            {
                int @int;
                if (int.TryParse(string_value, out @int))
                    result.Add(@int);
                else
                    result.Add(default);
            }

            return result;
        }
    }
}