using SAM.Analytical.Tas.OptGen.Attributes;
using System.Reflection;

namespace SAM.Analytical.Tas.OptGen
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
    }
}