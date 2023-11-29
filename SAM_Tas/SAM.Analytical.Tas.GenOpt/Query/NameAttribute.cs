using SAM.Analytical.Tas.GenOpt.Attributes;
using System;
using System.Reflection;

namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Query
    {
        public static NameAttribute NameAttribute(this PropertyInfo propertyInfo)
        {
            if(propertyInfo == null)
            {
                return null;
            }

            object[] objects = propertyInfo.GetCustomAttributes(typeof(NameAttribute), false);
            if (objects == null || objects.Length == 0)
            {
                return null;
            }

            foreach (object @object in objects)
            {
                NameAttribute result = @object as NameAttribute;
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static NameAttribute NameAttribute(this IGenOptObject genOptObject)
        {
            return NameAttribute(genOptObject?.GetType());
        }

        public static NameAttribute NameAttribute(this Type type)
        {
            if(type == null)
            {
                return null;
            }

            object[] objects = type.GetCustomAttributes(typeof(NameAttribute), false);
            if (objects == null || objects.Length == 0)
            {
                return null;
            }

            foreach (object @object in objects)
            {
                NameAttribute result = @object as NameAttribute;
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}