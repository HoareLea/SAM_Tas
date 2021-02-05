using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<T> Results<T>(this IEnumerable<Core.IResult> results, T result) where T : Core.Result
        {
            if (results == null || results.Count() == 0 || result == null)
                return null;

            LoadType loadType = Analytical.Query.LoadType(result as dynamic);
            string reference = result.Reference;

            List<T> _result = new List<T>();
            foreach(Core.Result result_Temp in results)
            {
                T result_Temp_1 = result_Temp as T;
                if (result_Temp_1 == null)
                    continue;

                if (reference != null && result_Temp_1.Reference != reference)
                    continue;

                if (Analytical.Query.LoadType(result_Temp_1 as dynamic) != loadType)
                    continue;

                _result.Add(result_Temp_1);
            }

            return _result;
        }
    }
}