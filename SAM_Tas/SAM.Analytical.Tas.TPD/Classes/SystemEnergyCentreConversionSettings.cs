﻿using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemEnergyCentreConversionSettings : IJSAMObject
    {
        public bool Simulate { get; set; } = true;
        public int StartHour { get; set; } = 0;
        public int EndHour { get; set; } = 8759;
        public bool IncludeResults { get; set; } = true;

        public SystemEnergyCentreConversionSettings() 
        { 
        }

        public SystemEnergyCentreConversionSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public SystemEnergyCentreConversionSettings(SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings)
        {
            if (systemEnergyCentreConversionSettings != null)
            {
                Simulate = systemEnergyCentreConversionSettings.Simulate;
                StartHour = systemEnergyCentreConversionSettings.StartHour;
                EndHour = systemEnergyCentreConversionSettings.EndHour;
                IncludeResults = systemEnergyCentreConversionSettings.IncludeResults;
            }
        }

        public ComponentConversionSettings GetComponentConversionSettings()
        {
            return new ComponentConversionSettings() { StartHour = StartHour, EndHour = EndHour, IncludeResults = IncludeResults };
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Simulate"))
            {
                Simulate = jObject.Value<bool>("Simulate");
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

            result.Add("Simulate", Simulate);

            result.Add("StartHour", StartHour);
            result.Add("EndHour", EndHour);

            result.Add("IncludeResults", IncludeResults);

            return result;
        }
    }
}
