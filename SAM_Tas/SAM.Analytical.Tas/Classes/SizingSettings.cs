using Newtonsoft.Json.Linq;
using SAM.Core;

namespace SAM.Analytical.Tas
{
    public class SizingSettings : IJSAMObject
    {
        public bool ExcludeOutdoorAir { get; set; } = false;

        public bool ExcludePositiveInternalGains { get; set; } = false;

        public bool GenerateUncappedFile { get; set; } = true;

        public bool GenerateHDDCDDFile { get; set; } = true;

        public bool SystemSizingMethod { get; set; } = false;

        public SizingSettings()
        {

        }

        public SizingSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public SizingSettings(bool excludeOutdoorAir, bool excludePositiveInternalGains, bool generateUncappedFile, bool generateHDDCDDFile, bool systemSizingMethod)
        {
            ExcludeOutdoorAir = excludeOutdoorAir;
            ExcludePositiveInternalGains = excludePositiveInternalGains;
            GenerateUncappedFile = generateUncappedFile;
            GenerateHDDCDDFile = generateHDDCDDFile;
            SystemSizingMethod = systemSizingMethod;
        }

        public SizingSettings(SizingSettings sizingSettings)
        {
            if(sizingSettings != null)
            {
                ExcludeOutdoorAir = sizingSettings.ExcludeOutdoorAir;
                ExcludePositiveInternalGains = sizingSettings.ExcludePositiveInternalGains;
                GenerateUncappedFile = sizingSettings.GenerateUncappedFile;
                GenerateHDDCDDFile = sizingSettings.GenerateHDDCDDFile;
                SystemSizingMethod = sizingSettings.SystemSizingMethod;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("ExcludeOutdoorAir"))
            {
                ExcludeOutdoorAir = jObject.Value<bool>("ExcludeOutdoorAir");
            }

            if (jObject.ContainsKey("ExcludePositiveInternalGains"))
            {
                ExcludePositiveInternalGains = jObject.Value<bool>("ExcludePositiveInternalGains");
            }

            if (jObject.ContainsKey("GenerateUncappedFile"))
            {
                GenerateUncappedFile = jObject.Value<bool>("GenerateUncappedFile");
            }

            if (jObject.ContainsKey("GenerateHDDCDDFile"))
            {
                GenerateHDDCDDFile = jObject.Value<bool>("GenerateHDDCDDFile");
            }

            if (jObject.ContainsKey("SystemSizingMethod"))
            {
                SystemSizingMethod = jObject.Value<bool>("SystemSizingMethod");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));
            
            jObject.Add("ExcludeOutdoorAir", ExcludeOutdoorAir);
            jObject.Add("ExcludePositiveInternalGains", ExcludePositiveInternalGains);
            jObject.Add("GenerateUncappedFile", GenerateUncappedFile);
            jObject.Add("GenerateHDDCDDFile", GenerateHDDCDDFile);
            jObject.Add("SystemSizingMethod", SystemSizingMethod);

            return jObject;
        }
    }
}
