using System.Collections;
using System.Collections.Generic;
using TSD;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static List<T> AnnualBuildingResult<T>(this BuildingData buildingData, tsdBuildingArray tsdBuildingArray)
        {
            if (buildingData == null)
                return null;

            IEnumerable enumerable = buildingData.GetAnnualBuildingResult((int)tsdBuildingArray) as IEnumerable;
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
    }
}