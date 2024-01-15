using SAM.Analytical.Tas.GenOpt.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.GenOpt
{
    public static partial class Query
    {
        public static string Text(this IEnumerable enumerable)
        {
            if(enumerable == null)
            {
                return null;
            }

            List<string> texts = new List<string>();
            foreach(object @object in enumerable)
            {
                IGenOptObject genOptObject = @object as IGenOptObject;
                if(genOptObject == null)
                {
                    continue;
                }

                string text = genOptObject.Text;
                if (text == null)
                {
                    continue;
                }

                NameAttribute nameAttribute = NameAttribute(genOptObject);
                if(nameAttribute == null || string.IsNullOrWhiteSpace(nameAttribute.Name))
                {
                    texts.Add(text);
                }
                else
                {
                    texts.Add(string.Format("{0} {{\n{1}\n}}", nameAttribute.Name, text));
                }
            }

            return string.Format("{0}\n", string.Join("\n", texts));
        }
    }
}