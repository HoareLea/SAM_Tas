using System.Collections;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<T> AnnualSurfaceResult<T>(this SurfaceData surfaceData, tsdSurfaceArray tsdSurfaceArray)
        {
            if (surfaceData == null)
                return null;

            IEnumerable enumerable = surfaceData.GetAnnualSurfaceResult((int)tsdSurfaceArray) as IEnumerable;
            if (enumerable == null)
                return null;

            List<T> result = new List<T>();
            foreach(object @object in enumerable)
            {
                if (@object == null)
                {
                    result.Add(default);
                }
                else if (@object is T)
                {
                    result.Add((T)@object);
                }
                else
                {
                    T value;
                    if (!Core.Query.TryConvert(@object, out value))
                        result.Add(default);
                    else
                        result.Add(value);
                }
            }

            return result;
        }

        public static List<T> AnnualSurfaceResult<T>(this SurfaceData surfaceData, ResultType resultType) 
        {
            if (surfaceData == null || resultType == Tas.ResultType.Undefined)
                return null;

            return AnnualSurfaceResult<T>(surfaceData, resultType.TsdSurfaceArray().Value);
        }
    }
}