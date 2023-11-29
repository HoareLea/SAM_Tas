using SAM.Analytical.Tas.GenOpt.Attributes;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SAM.Analytical.Tas.GenOpt
{
    public abstract class GenOptObject : IGenOptObject
    {
        public string Text
        {
            get
            {
                return GetText();
            }
        }

        protected virtual string GetText()
        {
            PropertyInfo[] propertyInfos = GetType()?.GetProperties();
            if(propertyInfos == null || propertyInfos.Length == 0)
            {
                return null;
            }

            List<string> texts = new List<string>();
            foreach(PropertyInfo propertyInfo in propertyInfos)
            {
                NameAttribute nameAttribute = propertyInfo.NameAttribute();
                if(nameAttribute == null)
                {
                    continue;
                }

                string name = nameAttribute.Name;
                if(string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }


                object value = propertyInfo.GetValue(this);

                string text = null;
                if(value is IGenOptObject)
                {
                    text = string.Format("{0} {{\n{1}\n}}\n", nameAttribute.Name, ((IGenOptObject)value).Text);
                }
                else if(value is IEnumerable && !(value is string))
                {
                    text = Query.Text((IEnumerable)value);
                    if(text != null)
                    {
                        text = string.Format("{0} {{\n{1}\n}}\n", nameAttribute.Name, text);
                    }
                }
                else
                {
                    value = value is bool ? value.ToString().ToLower() : value.ToString();

                    QuotedValueAttribute quotedValueAttribute = Query.QuotedValueAttribute(propertyInfo);
                    if(quotedValueAttribute != null)
                    {
                        value = string.Format("\"{0}\"", value);
                    }

                    text = string.Format("{0} = {1};", nameAttribute.Name, value);
                }

                if(string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                texts.Add(text);

            }

            return string.Join("\n", texts);
        }
    }
}
