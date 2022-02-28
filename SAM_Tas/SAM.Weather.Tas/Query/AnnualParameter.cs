using System.Collections;
using System.Collections.Generic;

namespace SAM.Weather.Tas
{
    public static partial class Query
    {
        public static List<T> AnnualParameter<T>(this TWD.WeatherYear weatherYear, int index)
        {
            if (weatherYear == null || index == -1)
            {
                return null;
            }

            IEnumerable enumerable = weatherYear.GetAnnualParameter(index) as IEnumerable;
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

        public static List<T> AnnualParameter<T>(this TWD.WeatherYear weatherYear, WeatherDataType weatherDataType)
        {
            if(weatherYear == null)
            {
                return null;
            }

            int index = AnnualParameterIndex(weatherDataType);
            if (index == -1)
            {
                return null;
            }

            return AnnualParameter<T>(weatherYear, index);
        }

        public static List<T> AnnualParameter<T>(this TBD.WeatherYear weatherYear, WeatherDataType weatherDataType)
        {
            int index = AnnualParameterIndex(weatherDataType);
            if (index == -1)
            {
                return null;
            }

            IEnumerable enumerable = weatherYear.GetAnnualParameter(index) as IEnumerable;
            if (enumerable == null)
                return null;

            List<T> result = new List<T>();
            foreach (object @object in enumerable)
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