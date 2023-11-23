using SAM.Analytical.Tas.OptGen.Attributes;
using SAM.Analytical.Tas.OptGen.Interfaces;
using System;
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

                NameAttribute nameAttribute = Query.NameAttribute(optGenObject);
                if(nameAttribute == null || string.IsNullOrWhiteSpace(nameAttribute.Name))
                {
                    continue;
                }

                string text = optGenObject.Text;
                if(text == null)
                {
                    continue;
                }

                texts.Add(string.Format("{0} {\n{1}\n}", nameAttribute.Name, text));
            }

            return string.Join("\n", texts);
        }
    }
}