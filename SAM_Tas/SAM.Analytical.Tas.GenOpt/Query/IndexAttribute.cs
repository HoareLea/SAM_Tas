using SAM.Analytical.Tas.GenOpt.Attributes;
using System.Reflection;

namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Query
    {
        public static IndexAttribute IndexAttribute(this PropertyInfo propertyInfo)
        {
            if(propertyInfo == null)
            {
                return null;
            }

            object[] objects = propertyInfo.GetCustomAttributes(typeof(IndexAttribute), false);
            if (objects == null || objects.Length == 0)
            {
                return null;
            }

            foreach (object @object in objects)
            {
                IndexAttribute result = @object as IndexAttribute;
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}