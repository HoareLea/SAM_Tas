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

        public static List<T> AnnualSurfaceResult<T>(this SurfaceData surfaceData, PanelDataType panelDataType) 
        {
            if (surfaceData == null || panelDataType == Tas.PanelDataType.Undefined)
                return null;

            return AnnualSurfaceResult<T>(surfaceData, panelDataType.TsdSurfaceArray().Value);
        }

        public static T AnnualSurfaceResult<T>(this SurfaceData surfaceData, tsdSurfaceArray tsdSurfaceArray, int index, T @default = default)
        {
            if(surfaceData == null || index == -1)
            {
                return @default;
            }

            IEnumerable enumerable = surfaceData.GetAnnualSurfaceResult((int)tsdSurfaceArray) as IEnumerable;
            if (enumerable == null)
            {
                return @default;
            }

            int i = 0;
            foreach (object @object in enumerable)
            {
                if(i != index)
                {
                    i++;
                    continue;
                }

                if (@object == null)
                {
                    return @default;
                }
                else if (@object is T)
                {
                    return (T)@object;
                }
                else
                {
                    if (!Core.Query.TryConvert(@object, out T value))
                    {
                        return @default;
                    }

                    return value;
                }
            }

            return @default;
        }

        public static T AnnualSurfaceResult<T>(this SurfaceData surfaceData, PanelDataType panelDataType, int index, T @default = default)
        {
            if (surfaceData == null || panelDataType == Tas.PanelDataType.Undefined)
            {
                return @default;
            }

            return AnnualSurfaceResult<T>(surfaceData, panelDataType.TsdSurfaceArray().Value, index);
        }
    }
}