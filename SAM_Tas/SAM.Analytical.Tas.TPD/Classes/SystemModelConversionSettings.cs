using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public class SystemModelConversionSettings : IJSAMObject
    {
        public bool Simulate { get; set; } = true;
        public int StartHour { get; set; } = 0;
        public int EndHour { get; set; } = 8759;

        public SystemModelConversionSettings() 
        { 
        }

        public SystemModelConversionSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public SystemModelConversionSettings(SystemModelConversionSettings systemModelConversionSettings)
        {
            if (systemModelConversionSettings != null)
            {
                Simulate = systemModelConversionSettings.Simulate;
                StartHour = systemModelConversionSettings.StartHour;
                EndHour = systemModelConversionSettings.EndHour;
            }
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

            return true;
        }

        public JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            result.Add("Simulate", Simulate);

            result.Add("StartHour", StartHour);
            result.Add("EndHour", EndHour);

            return result;
        }
    }
}
