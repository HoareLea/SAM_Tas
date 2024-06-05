﻿using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public class ComponentConversionSettings : IJSAMObject
    {
        public int StartHour { get; set; } = 0;
        public int EndHour { get; set; } = 8759;
        public bool IncludeResults { get; set; } = true;

        public ComponentConversionSettings() 
        { 
        }

        public ComponentConversionSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ComponentConversionSettings(ComponentConversionSettings componentConversionSettings)
        {
            if (componentConversionSettings != null)
            {
                StartHour = componentConversionSettings.StartHour;
                EndHour = componentConversionSettings.EndHour;
                IncludeResults = componentConversionSettings.IncludeResults;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("StartHour"))
            {
                StartHour = jObject.Value<int>("StartHour");
            }

            if (jObject.ContainsKey("EndHour"))
            {
                EndHour = jObject.Value<int>("EndHour");
            }

            if (jObject.ContainsKey("IncludeResults"))
            {
                IncludeResults = jObject.Value<bool>("IncludeResults");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            result.Add("StartHour", StartHour);
            result.Add("EndHour", EndHour);

            result.Add("IncludeResults", IncludeResults);

            return result;
        }
    }
}
