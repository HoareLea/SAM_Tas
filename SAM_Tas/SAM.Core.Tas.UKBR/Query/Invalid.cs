using System;

namespace SAM.Core.Tas.UKBR
{
    public static partial class Query
    {
        public static T Invalid<T>()
        {
            if(typeof(T) == typeof(int))
            {
                return (T)(object)-9999;
            }

            if (typeof(T) == typeof(double))
            {
                return (T)(object)-9999.0;
            }

            if (typeof(T) == typeof(Guid))
            {
                return (T)(object)Guid.Empty;
            }

            if (typeof(T) == typeof(string))
            {
                return default(T);
            }

            return default(T);
        }
    }
}