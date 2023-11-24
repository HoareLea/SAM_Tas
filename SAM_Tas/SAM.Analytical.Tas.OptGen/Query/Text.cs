using SAM.Analytical.Tas.OptGen.Attributes;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.OptGen
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
                IOptGenObject optGenObject = @object as IOptGenObject;
                if(optGenObject == null)
                {
                    continue;
                }

                string text = optGenObject.Text;
                if (text == null)
                {
                    continue;
                }

                NameAttribute nameAttribute = Query.NameAttribute(optGenObject);
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