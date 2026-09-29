// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors
using System.Text.Json.Nodes;
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

        /// <summary>
        /// Refuse - convert nothing - unless the TSD's simulation covers days 1..365
        /// (<see cref="Query.FullYearRefusal(TSD.SimulationData)"/>). False, the default, keeps every part-year TSD
        /// convertible as before; a caller whose assessment is defined over a whole year (Part O's TM59) sets it,
        /// because the length of the series cannot show a part year: TSD pads the days it did not simulate with -1.
        /// </summary>
        public bool RequireFullYear { get; set; } = false;

        public TSDConversionSettings()
        {

        }

        public TSDConversionSettings(JsonObject jObject)
        {
            FromJsonObject(jObject);
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
                RequireFullYear = tSDConversionSettings.RequireFullYear;
            }
        }

        public bool FromJsonObject(JsonObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("SpaceDataTypes"))
            {
                JsonArray jArray = jObject["SpaceDataTypes"] as JsonArray;
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
                JsonArray jArray = jObject["PanelDataTypes"] as JsonArray;
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
                JsonArray jArray = jObject["SpaceNames"] as JsonArray;
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
                JsonArray jArray = jObject["ZoneNames"] as JsonArray;
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
                ConvertWeaterData = jObject["ConvertWeaterData"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("ConvertZones"))
            {
                ConvertZones = jObject["ConvertZones"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("RequireFullYear"))
            {
                RequireFullYear = jObject["RequireFullYear"]?.GetValue<bool>() ?? default(bool);
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (SpaceDataTypes != null)
            {
                JsonArray jArray = new JsonArray();
                foreach (SpaceDataType spaceDataType in SpaceDataTypes)
                {
                    jArray.Add(spaceDataType.ToString());
                }

                jObject.Add("SpaceDataTypes", jArray);
            }

            if (PanelDataTypes != null)
            {
                JsonArray jArray = new JsonArray();
                foreach (PanelDataType panelDataType in PanelDataTypes)
                {
                    jArray.Add(panelDataType.ToString());
                }

                jObject.Add("PanelDataTypes", jArray);
            }

            if (SpaceNames != null)
            {
                JsonArray jArray = new JsonArray();
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
                JsonArray jArray = new JsonArray();
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

            //Written only when set, so every existing serialized setting is unchanged.
            if (RequireFullYear)
            {
                jObject.Add("RequireFullYear", RequireFullYear);
            }

            return jObject;
        }
    }
}
