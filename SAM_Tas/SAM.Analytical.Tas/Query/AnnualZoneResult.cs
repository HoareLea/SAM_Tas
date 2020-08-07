using System.Collections;
using System.Collections.Generic;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static List<T> AnnualZoneResult<T>(this ZoneData zoneData, tsdZoneArray tsdZoneArray)
        {
            if (zoneData == null)
                return null;

            IEnumerable enumerable = zoneData.GetAnnualZoneResult((int)tsdZoneArray) as IEnumerable;
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

        public static List<T> AnnualZoneResult<T>(this ZoneData zoneData, SpaceDataType spaceDataType) 
        {
            if (zoneData == null || spaceDataType == Tas.SpaceDataType.Undefined)
                return null;

            return AnnualZoneResult<T>(zoneData, spaceDataType.TsdZoneArray().Value);
        }
    }
}