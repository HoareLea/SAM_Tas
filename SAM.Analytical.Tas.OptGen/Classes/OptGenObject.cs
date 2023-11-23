using SAM.Analytical.Tas.OptGen.Attributes;
using SAM.Analytical.Tas.OptGen.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SAM.Analytical.Tas.OptGen
{
    public class OptGenObject : IOptGenObject
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
                if(value is OptGenObject)
                {
                    text = string.Format("{0} {\n {1}\n}\n", nameAttribute.Name, ((OptGenObject)value).Text);
                }
                else
                {
                    text = string.Format("{0} = {1};", nameAttribute.Name, value.ToString());
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
