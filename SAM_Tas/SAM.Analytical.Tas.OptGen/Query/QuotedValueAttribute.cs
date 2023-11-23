using SAM.Analytical.Tas.OptGen.Attributes;
using System.Reflection;

namespace SAM.Analytical.Tas.OptGen
{
    public static partial class Query
    {
        public static QuotedValueAttribute QuotedValueAttribute(this PropertyInfo propertyInfo)
        {
            if(propertyInfo == null)
            {
                return null;
            }

            object[] objects = propertyInfo.GetCustomAttributes(typeof(QuotedValueAttribute), false);
            if (objects == null || objects.Length == 0)
            {
                return null;
            }

            foreach (object @object in objects)
            {
                QuotedValueAttribute result = @object as QuotedValueAttribute;
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}