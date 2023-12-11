using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public abstract class SystemIndexedResult : Result, ISystemResult
    {
        private Dictionary<string, IndexedDoubles> dictionary;

        public SystemIndexedResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }

        public SystemIndexedResult(SystemIndexedResult systemIndexedResult)
            : base(systemIndexedResult)
        {
            if(systemIndexedResult?.dictionary != null)
            {
                dictionary = new Dictionary<string, IndexedDoubles>();
                foreach (KeyValuePair<string, IndexedDoubles> keyValuePair in systemIndexedResult.dictionary)
                {
                    dictionary[keyValuePair.Key] = keyValuePair.Value == null ? null : new IndexedDoubles(keyValuePair.Value);
                }
            }
        }

        public SystemIndexedResult(string uniqueId, string name, string source, Dictionary<string, IndexedDoubles> dictionary)
            : base(name, source, uniqueId)
        {
            if (dictionary != null)
            {
                this.dictionary = new Dictionary<string, IndexedDoubles>();
                foreach (KeyValuePair<string, IndexedDoubles> keyValuePair in dictionary)
                {
                    this.dictionary[keyValuePair.Key] = keyValuePair.Value == null ? null : new IndexedDoubles(keyValuePair.Value);
                }
            }
        }

        public List<string> Keys
        {
            get
            {
                return dictionary?.Keys?.ToList();
            }
        }

        public List<IndexedDoubles> Values
        {
            get
            {
                return dictionary?.Values?.ToList();
            }
        }

        public IndexedDoubles this[string key]
        {
            get
            {
                if(key == null || dictionary == null)
                {
                    return null;
                }

                return dictionary[key];
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return result;
            }

            if (jObject.ContainsKey("Data"))
            {
                JArray jArray = jObject.Value<JArray>("Data");
                if (jArray != null)
                {
                    dictionary = new Dictionary<string, IndexedDoubles>();
                    foreach (JArray jArray_Temp in jArray)
                    {
                        if (jArray_Temp == null || jArray_Temp.Count != 2)
                        {
                            continue;
                        }

                        string uniqueId = (string)jArray_Temp[0];
                        if (uniqueId == null)
                        {
                            continue;
                        }

                        JObject jObject_Temp = jArray_Temp[1] is JObject ? (JObject)jArray_Temp[1] : null;

                        dictionary[uniqueId] = new IndexedDoubles(jObject_Temp);
                    }
                }
            }

            return result;
        }

        public override JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if (jObject == null)
            {
                return null;
            }

            if (dictionary != null)
            {
                JArray jArray = new JArray();
                foreach (KeyValuePair<string, IndexedDoubles> keyValuePair in dictionary)
                {
                    if (keyValuePair.Key == null)
                    {
                        continue;
                    }

                    jArray.Add(new JArray(keyValuePair.Key, keyValuePair.Value?.ToJObject()));
                }
                jObject.Add("Data", jArray);
            }

            return jObject;
        }
    }
}
