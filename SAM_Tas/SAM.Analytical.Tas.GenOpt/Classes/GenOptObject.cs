﻿using SAM.Analytical.Tas.GenOpt.Attributes;
using System;
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

            List<Tuple<int, PropertyInfo>> tuples = new List<Tuple<int, PropertyInfo>>();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                NameAttribute nameAttribute = propertyInfo.NameAttribute();
                if (nameAttribute == null)
                {
                    continue;
                }

                string name = nameAttribute.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                int index = int.MaxValue;
                IndexAttribute indexAttribute = propertyInfo.IndexAttribute();
                if (indexAttribute != null)
                {
                    index = indexAttribute.Index;
                }

                tuples.Add(new Tuple<int, PropertyInfo>(index, propertyInfo));
            }

            if(tuples == null || tuples.Count == 0)
            {
                return null;
            }

            tuples.Sort((x, y) => x.Item1.CompareTo(y.Item1));

            List<string> texts = new List<string>();
            foreach(PropertyInfo propertyInfo in tuples.ConvertAll(x => x.Item2))
            {
                string name = propertyInfo.NameAttribute()?.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                object value = propertyInfo.GetValue(this);

                string text = null;
                if(value is IGenOptObject)
                {
                    text = string.Format("{0} {{\n{1}\n}}\n", name, ((IGenOptObject)value).Text);
                }
                else if(value is IEnumerable && !(value is string))
                {
                    text = Query.Text((IEnumerable)value);
                    if(text != null)
                    {
                        text = string.Format("{0} {{\n{1}\n}}\n", name, text);
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

                    text = string.Format("{0} = {1};", name, value);
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
