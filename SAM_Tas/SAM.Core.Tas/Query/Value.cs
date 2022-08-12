using System.Xml.Linq;

namespace SAM.Core.Tas
{
    public static partial class Query
    {
        public static T Value<T>(this XAttribute xAttribute)
        {
            T result = default(T);

            if (typeof(T) == typeof(double))
            {
                if(xAttribute == null || string.IsNullOrWhiteSpace(xAttribute.Value))
                {
                    return (T)(object)double.NaN;
                }

                if(!Core.Query.TryConvert(xAttribute?.Value, out result))
                {
                    return (T)(object)double.NaN;
                }

                return (T)(object)result;
            }

            if (typeof(T) == typeof(string))
            {
                if (xAttribute == null)
                {
                    return default(T);
                }

                if (!Core.Query.TryConvert(xAttribute?.Value, out result))
                {
                    return default(T);
                }

                return (T)(object)result;
            }

            if (typeof(T) == typeof(bool))
            {
                if (xAttribute == null || string.IsNullOrWhiteSpace(xAttribute.Value))
                {
                    return default(T);
                }

                if (Core.Query.TryConvert(xAttribute.Value, out result))
                {
                    return (T)(object)result;
                }

                return (T)(object)(xAttribute.Value.Trim().ToUpper() == "TRUE");
            }

            if (Core.Query.TryConvert(xAttribute?.Value, out result))
            {
                return (T)(object)result;
            }

            return default(T);
        }
    }
}