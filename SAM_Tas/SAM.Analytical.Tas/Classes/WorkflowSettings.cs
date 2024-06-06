using Newtonsoft.Json.Linq;
using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public class WorkflowSettings : IJSAMObject
    {
        public string Path_TBD { get; set; } = null;

        public string Path_gbXML { get; set; } = null;

        public Weather.WeatherData WeatherData { get; set; } = null;

        public List<DesignDay> DesignDays_Heating { get; set; } = null;

        public List<DesignDay> DesignDays_Cooling { get; set; } = null;

        public List<SurfaceOutputSpec> SurfaceOutputSpecs { get; set; } = null;

        public bool UnmetHours { get; set; } = true;

        public bool Simulate { get; set; } = true;

        public bool Sizing { get; set; } = true;

        public bool UpdateZones { get; set; } = true;

        public bool UseWidths { get; set; } = false;

        public bool AddIZAMs { get; set; } = true;

        public int SimulateFrom { get; set; } = 1;

        public int SimulateTo { get; set; } = 1;

        public bool RemoveExistingTBD { get; set; } = false;

        public WorkflowSettings()
        {

        }

        public WorkflowSettings(JObject jObject)
        {
            FromJObject(jObject);
        }

        public WorkflowSettings(WorkflowSettings workflowSettings)
        {
            if(workflowSettings != null)
            {
                Path_TBD = workflowSettings.Path_TBD;
                Path_gbXML = workflowSettings.Path_gbXML;
                WeatherData = workflowSettings.WeatherData;
                DesignDays_Heating = workflowSettings.DesignDays_Heating;
                DesignDays_Cooling = workflowSettings.DesignDays_Cooling;
                SurfaceOutputSpecs = workflowSettings.SurfaceOutputSpecs;
                UnmetHours = workflowSettings.UnmetHours;
                Simulate = workflowSettings.Simulate;
                Sizing = workflowSettings.Sizing;
                UpdateZones = workflowSettings.UpdateZones;
                UseWidths = workflowSettings.UseWidths;
                AddIZAMs = workflowSettings.AddIZAMs;
                SimulateFrom = workflowSettings.SimulateFrom;
                SimulateTo = workflowSettings.SimulateTo;

                RemoveExistingTBD = workflowSettings.RemoveExistingTBD;
            }
        }

        public bool FromJObject(JObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("Path_TBD"))
            {
                Path_TBD = jObject.Value<string>("Path_TBD");
            }

            if (jObject.ContainsKey("Path_gbXML"))
            {
                Path_gbXML = jObject.Value<string>("Path_gbXML");
            }

            if (jObject.ContainsKey("WeatherData"))
            {
                WeatherData = new Weather.WeatherData(jObject.Value<JObject>("WeatherData"));
            }

            if (jObject.ContainsKey("DesignDays_Heating"))
            {
                JArray jArray = jObject.Value<JArray>("DesignDays_Heating");
                if(jArray != null)
                {
                    DesignDays_Heating = new List<DesignDay>();
                    foreach(JObject jObject_DesignDay in jArray)
                    {
                        DesignDays_Heating.Add(new DesignDay(jObject_DesignDay));
                    }
                }
            }

            if (jObject.ContainsKey("DesignDays_Cooling"))
            {
                JArray jArray = jObject.Value<JArray>("DesignDays_Cooling");
                if (jArray != null)
                {
                    DesignDays_Cooling = new List<DesignDay>();
                    foreach (JObject jObject_DesignDay in jArray)
                    {
                        DesignDays_Cooling.Add(new DesignDay(jObject_DesignDay));
                    }
                }
            }

            if (jObject.ContainsKey("SurfaceOutputSpecs"))
            {
                JArray jArray = jObject.Value<JArray>("SurfaceOutputSpecs");
                if (jArray != null)
                {
                    SurfaceOutputSpecs = new List<SurfaceOutputSpec>();
                    foreach (JObject jObject_SurfaceOutputSpec in jArray)
                    {
                        SurfaceOutputSpecs.Add(new SurfaceOutputSpec(jObject_SurfaceOutputSpec));
                    }
                }
            }


            if (jObject.ContainsKey("UnmetHours"))
            {
                UnmetHours = jObject.Value<bool>("UnmetHours");
            }

            if (jObject.ContainsKey("Simulate"))
            {
                Simulate = jObject.Value<bool>("Simulate");
            }

            if (jObject.ContainsKey("Sizing"))
            {
                Sizing = jObject.Value<bool>("Sizing");
            }

            if (jObject.ContainsKey("UpdateZones"))
            {
                UpdateZones = jObject.Value<bool>("UpdateZones");
            }

            if (jObject.ContainsKey("UseWidths"))
            {
                UseWidths = jObject.Value<bool>("UseWidths");
            }

            if (jObject.ContainsKey("AddIZAMs"))
            {
                AddIZAMs = jObject.Value<bool>("AddIZAMs");
            }

            if (jObject.ContainsKey("SimulateFrom"))
            {
                SimulateFrom = jObject.Value<int>("SimulateFrom");
            }

            if (jObject.ContainsKey("SimulateTo"))
            {
                SimulateTo = jObject.Value<int>("SimulateTo");
            }

            if (jObject.ContainsKey("RemoveExistingTBD"))
            {
                RemoveExistingTBD = jObject.Value<bool>("RemoveExistingTBD");
            }

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));
            
            if(Path_TBD != null)
            {
                jObject.Add("Path_TBD", Path_TBD);
            }

            if (Path_gbXML != null)
            {
                jObject.Add("Path_gbXML", Path_gbXML);
            }

            if (WeatherData != null)
            {
                jObject.Add("WeatherData", WeatherData.ToJObject());
            }

            if (DesignDays_Heating != null)
            {
                JArray jArray = new JArray();
                foreach(DesignDay designDay in DesignDays_Heating)
                {
                    jArray.Add(designDay.ToJObject());
                }

                jObject.Add("DesignDays_Heating", jArray);
            }

            if (DesignDays_Cooling != null)
            {
                JArray jArray = new JArray();
                foreach (DesignDay designDay in DesignDays_Cooling)
                {
                    jArray.Add(designDay.ToJObject());
                }

                jObject.Add("DesignDays_Cooling", jArray);
            }

            if (SurfaceOutputSpecs != null)
            {
                JArray jArray = new JArray();
                foreach (SurfaceOutputSpec surfaceOutputSpec in SurfaceOutputSpecs)
                {
                    jArray.Add(surfaceOutputSpec.ToJObject());
                }

                jObject.Add("SurfaceOutputSpecs", jArray);
            }

            jObject.Add("UnmetHours", UnmetHours);
            jObject.Add("Simulate", Simulate);
            jObject.Add("Sizing", Sizing);
            jObject.Add("UpdateZones", UpdateZones);
            jObject.Add("UseWidths", UseWidths);
            jObject.Add("AddIZAMs", AddIZAMs);

            jObject.Add("SimulateFrom", SimulateFrom);
            jObject.Add("SimulateTo", SimulateTo);

            jObject.Add("RemoveExistingTBD", RemoveExistingTBD);

            return jObject;
        }
    }
}
