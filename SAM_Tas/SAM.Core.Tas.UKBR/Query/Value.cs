using System.Xml.Linq;

namespace SAM.Core.Tas.UKBR
{
    public static partial class Query
    {
        public static T Value<T>(this XAttribute xAttribute)
        {
            if(!Core.Query.TryGetValue(xAttribute, out T result))
            {
                if(typeof(T) == typeof(global::System.Guid))
                {
                    return (T)(object)global::System.Guid.Empty;
                }

                if (typeof(T) == typeof(double))
                {
                    return (T)(object)double.NaN;
                }

                if (typeof(T) == typeof(int))
                {
                    return (T)(object)int.MinValue;
                }

                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)false;
                }
            }

            return result;
        }

        public static T Value<T>(this XAttribute xAttribute, T defaultValue)
        {
            if(xAttribute == null)
            {
                return defaultValue;
            }

            if(!Core.Query.TryGetValue(xAttribute, out T result))
            {
                return defaultValue;
            }

            return result;
        }
    }
}