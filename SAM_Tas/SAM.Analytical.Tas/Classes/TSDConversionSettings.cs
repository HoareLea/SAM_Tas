using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public class TSDConversionSettings : IJSAMObject
    {
        public HashSet<SpaceDataType> SpaceDataTypes { get; set; } = null;

        public HashSet<PanelDataType> PanelDataTypes { get; set; } = null;

        public HashSet<string> SpaceNames { get; set; } = null;

        public HashSet<string> ZoneNames { get; set; } = null;

        public bool ConvertWeaterData { get; set; } = true;

        public bool ConvertZones { get; set; } = false;

        public TSDConversionSettings()
        {

        }

        public TSDConversionSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public TSDConversionSettings(TSDConversionSettings tSDConversionSettings)
        {
            if(tSDConversionSettings != null)
            {
                SpaceDataTypes = tSDConversionSettings.SpaceDataTypes == null ? null : new HashSet<SpaceDataType>(tSDConversionSettings.SpaceDataTypes);
                PanelDataTypes = tSDConversionSettings.PanelDataTypes == null ? null : new HashSet<PanelDataType>(tSDConversionSettings.PanelDataTypes);
                ConvertWeaterData = tSDConversionSettings.ConvertWeaterData;
                ConvertZones = tSDConversionSettings.ConvertZones;
                SpaceNames = tSDConversionSettings.SpaceNames == null ? null : new HashSet<string>(tSDConversionSettings.SpaceNames);
                ZoneNames = tSDConversionSettings.ZoneNames == null ? null : new HashSet<string>(tSDConversionSettings.ZoneNames);
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("SpaceDataTypes"))
            {
                JArray jArray = jObject.Value<JArray>("SpaceDataTypes");
                if(jArray != null)
                {
                    SpaceDataTypes = new HashSet<SpaceDataType>();
                    foreach(string @string in jArray)
                    {
                        if(Core.Query.TryGetEnum(@string, out SpaceDataType spaceDataType))
                        {
                            SpaceDataTypes.Add(spaceDataType);
                        }
                    }
                }
            }

            if (jObject.ContainsKey("PanelDataTypes"))
            {
                JArray jArray = jObject.Value<JArray>("PanelDataTypes");
                if (jArray != null)
                {
                    PanelDataTypes = new HashSet<PanelDataType>();
                    foreach (string @string in jArray)
                    {
                        if (Core.Query.TryGetEnum(@string, out PanelDataType panelDataType))
                        {
                            PanelDataTypes.Add(panelDataType);
                        }
                    }
                }
            }

            if (jObject.ContainsKey("SpaceNames"))
            {
                JArray jArray = jObject.Value<JArray>("SpaceNames");
                if (jArray != null)
                {
                    SpaceNames = new HashSet<string>();
                    foreach (string @string in jArray)
                    {
                        SpaceNames.Add(@string);
                    }
                }
            }

            if (jObject.ContainsKey("ZoneNames"))
            {
                JArray jArray = jObject.Value<JArray>("ZoneNames");
                if (jArray != null)
                {
                    ZoneNames = new HashSet<string>();
                    foreach (string @string in jArray)
                    {
                        ZoneNames.Add(@string);
                    }
                }
            }

            if (jObject.ContainsKey("ConvertWeaterData"))
            {
                ConvertWeaterData = jObject.Value<bool>("ConvertWeaterData");
            }

            if (jObject.ContainsKey("ConvertZones"))
            {
                ConvertZones = jObject.Value<bool>("ConvertZones");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (SpaceDataTypes != null)
            {
                JArray jArray = new JArray();
                foreach (SpaceDataType spaceDataType in SpaceDataTypes)
                {
                    jArray.Add(spaceDataType.ToString());
                }

                jObject.Add("SpaceDataTypes", jArray);
            }

            if (PanelDataTypes != null)
            {
                JArray jArray = new JArray();
                foreach (PanelDataType panelDataType in PanelDataTypes)
                {
                    jArray.Add(panelDataType.ToString());
                }

                jObject.Add("PanelDataTypes", jArray);
            }

            if (SpaceNames != null)
            {
                JArray jArray = new JArray();
                foreach (string spaceName in SpaceNames)
                {
                    if(spaceName == null)
                    {
                        continue;
                    }

                    jArray.Add(spaceName);
                }

                jObject.Add("SpaceNames", jArray);
            }

            if (ZoneNames != null)
            {
                JArray jArray = new JArray();
                foreach (string zoneName in ZoneNames)
                {
                    if (zoneName == null)
                    {
                        continue;
                    }

                    jArray.Add(zoneName);
                }

                jObject.Add("ZoneNames", jArray);
            }

            jObject.Add("ConvertWeaterData", ConvertWeaterData);

            jObject.Add("ConvertZones", ConvertZones);

            return jObject;
        }
    }
}
