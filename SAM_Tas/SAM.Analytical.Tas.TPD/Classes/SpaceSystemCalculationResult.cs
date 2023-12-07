using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas.TPD
{
    public class SpaceSystemCalculationResult : Result
    {
        private double area;
        private double volume;

        private double heatingDuty;
        private double coolingDuty;
        private double designFlowRate;
        
        private Dictionary<string, IndexedDoubles> dictionary;

        public SpaceSystemCalculationResult(JObject jObject)
            : base(jObject)
        {
            FromJObject(jObject);
        }
        

        public SpaceSystemCalculationResult(SpaceSystemCalculationResult spaceSystemCalculationResult)
            : base(spaceSystemCalculationResult)
        {
            if(spaceSystemCalculationResult != null)
            {
                area= spaceSystemCalculationResult.area;
                volume= spaceSystemCalculationResult.volume;

                heatingDuty= spaceSystemCalculationResult.heatingDuty;
                coolingDuty = spaceSystemCalculationResult.coolingDuty;
                designFlowRate= spaceSystemCalculationResult.designFlowRate;

                if (spaceSystemCalculationResult.dictionary != null)
                {
                    dictionary = new Dictionary<string, IndexedDoubles>();
                    foreach (KeyValuePair<string, IndexedDoubles> keyValuePair in spaceSystemCalculationResult.dictionary)
                    {
                        dictionary[keyValuePair.Key] = keyValuePair.Value == null ? null : new IndexedDoubles(keyValuePair.Value);
                    }
                }
            }
        }

        public SpaceSystemCalculationResult(string uniqueId, string name, string source, double area, double volume, double heatingDuty, double coolingDuty, double designFlowRate, Dictionary<string, IndexedDoubles> dictionary)
            : base(name, source, uniqueId)
        {
            this.area = area;
            this.volume = volume;
            
            this.heatingDuty = heatingDuty;
            this.coolingDuty = coolingDuty;
            this.designFlowRate = designFlowRate;

            if(dictionary != null)
            {
                this.dictionary = new Dictionary<string, IndexedDoubles>();
                foreach(KeyValuePair<string, IndexedDoubles> keyValuePair in dictionary)
                {
                    this.dictionary[keyValuePair.Key] = keyValuePair.Value == null ? null : new IndexedDoubles(keyValuePair.Value);
                }
            }
        }

        public double Area
        {
            get
            {
                return area;
            }
        }

        public double Volume
        {
            get
            {
                return volume;
            }
        }

        public double HeatingDuty
        {
            get
            {
                return heatingDuty;
            }
        }

        public double CoolingDuty
        {
            get
            {
                return coolingDuty;
            }
        }

        public double DesignFlowRate
        {
            get
            {
                return designFlowRate;
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
        
        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if(!result)
            {
                return result;
            }

            if(jObject.ContainsKey("Area"))
            {
                area = jObject.Value<double>("Area");
            }

            if (jObject.ContainsKey("Volume"))
            {
                volume = jObject.Value<double>("Volume");
            }

            if (jObject.ContainsKey("HeatingDuty"))
            {
                heatingDuty = jObject.Value<double>("HeatingDuty");
            }

            if (jObject.ContainsKey("CoolingDuty"))
            {
                coolingDuty = jObject.Value<double>("CoolingDuty");
            }

            if (jObject.ContainsKey("DesignFlowRate"))
            {
                designFlowRate = jObject.Value<double>("DesignFlowRate");
            }

            if(jObject.ContainsKey("Data"))
            {
                JArray jArray = jObject.Value<JArray>("Data");
                if(jArray != null)
                {
                    dictionary = new Dictionary<string, IndexedDoubles>();
                    foreach (JArray jArray_Temp in jArray)
                    {
                        if(jArray_Temp == null || jArray_Temp.Count != 2)
                        {
                            continue;
                        }

                        string uniqueId = (string)jArray_Temp[0];
                        if(uniqueId == null)
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

        public JObject ToJObject()
        {
            JObject jObject = base.ToJObject();
            if(jObject == null)
            {
                return null;
            }

            if(double.IsNaN(area))
            {
                jObject.Add("Area", area);
            }

            if (double.IsNaN(volume))
            {
                jObject.Add("Volume", volume);
            }

            if (double.IsNaN(heatingDuty))
            {
                jObject.Add("HeatingDuty", heatingDuty);
            }

            if (double.IsNaN(coolingDuty))
            {
                jObject.Add("CoolingDuty", coolingDuty);
            }

            if (double.IsNaN(designFlowRate))
            {
                jObject.Add("DesignFlowRate", designFlowRate);
            }

            if(dictionary != null)
            {
                JArray jArray = new JArray();
                foreach (KeyValuePair<string, IndexedDoubles> keyValuePair in dictionary)
                {
                    if(keyValuePair.Key == null)
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
