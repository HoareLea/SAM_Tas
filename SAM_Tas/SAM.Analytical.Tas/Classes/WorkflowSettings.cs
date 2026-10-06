// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;
using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public class WorkflowSettings : IJSAMObject
    {
        public string Path_TBD { get; set; } = null;

        public string Path_gbXML { get; set; } = null;

        /// <summary>
        /// An already-converted TBD to <b>start this run from</b>, instead of converting the geometry
        /// again. Copied to <see cref="Path_TBD"/> before anything else runs; never itself written to.
        ///
        /// <para><b>What it is for</b></para>
        /// <para>
        /// An Approved Document O Iteration 2B optimisation runs the same thermal case ten times over ten
        /// designs, and between rounds only the <i>ventilation</i> state changes - the design airflow on
        /// each terminal, the balanced system duty, and the transfer/mechanical network rebuilt from them.
        /// The geometry, zones, surfaces, apertures, constructions and the shading calculation are
        /// identical every round, and on a real model they are the great majority of the work: measured on
        /// the licensed acceptance model, the conversion is 41.6 s of a 64.2 s round while the full-year
        /// simulation itself is 3.6 s.
        /// </para>
        /// <para>
        /// So a caller converts <b>once</b>, keeps that TBD as its canonical baseline, and hands it here for
        /// every later round.
        /// </para>
        ///
        /// <para><b>What is still done, and why that is the whole point</b></para>
        /// <para>
        /// Everything after the conversion runs exactly as it always does - the adiabatic and building
        /// element updates, <c>Modify.UpdateIds</c> (which stamps the TAS zone identities the assessment
        /// resolves results through), <c>UpdateZones</c>, the zone groups, <c>UpdateIZAMs</c>, sizing, a
        /// <b>real</b> full-year simulation, and the results. Nothing is skipped except the conversion of
        /// inputs that did not change, and nothing is reimplemented: this is the same method body, entered
        /// with a TBD that already exists.
        /// </para>
        ///
        /// <para><b>Set this or <see cref="Path_gbXML"/>, never both</b></para>
        /// <para>
        /// They are contradictory instructions - one says "convert the geometry", the other says "the
        /// geometry is already converted" - so a run given both is refused rather than silently preferring
        /// one. It is the caller's job to decide, and a caller that cannot show its canonical TBD is still
        /// valid for the current model must use the full conversion.
        /// </para>
        /// <para>
        /// <b>Whether the canonical TBD is still valid is not decided here.</b> This class cannot know what
        /// changed in the model since it was made; the caller proves compatibility and falls back to the
        /// full path where it cannot. What this does guarantee is that the canonical file is only ever read.
        /// </para>
        /// </summary>
        public string Path_TBD_Canonical { get; set; } = null;

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

        /// <summary>
        /// Removes every IZAM the TBD carries - inherited or otherwise - after the conversion steps.
        /// <para>
        /// <c>AddIZAMs = false</c> only skips <i>creation</i>; nothing removes IZAMs a warm start from
        /// <c>Path_TBD_Canonical</c>, or a reused TBD under <c>RemoveExistingTBD = false</c>, brought in. This
        /// sweeps them, which is what a genuinely IZAM-free thermal source needs.
        /// </para>
        /// <para><b>Defaults to false</b>, so no existing caller changes behaviour.</para>
        /// </summary>
        public bool RemoveIZAMs { get; set; } = false;

        /// <summary>
        /// Zeroes the mechanical ventilation gain - the <c>ticV</c> profile - on every internal condition.
        /// <para>
        /// Infiltration (<c>ticI</c>) and natural ventilation (aperture types and their opening schedules)
        /// are untouched: they are structurally distinct carriers, so no heuristic is needed to tell them
        /// apart. See <c>Query/ProfileReuseIndex.cs</c> for the slot-to-type authority.
        /// </para>
        /// <para><b>Defaults to false</b>, so no existing caller changes behaviour.</para>
        /// </summary>
        public bool RemoveMechanicalVentilationGains { get; set; } = false;

        public int SimulateFrom { get; set; } = 1;

        public int SimulateTo { get; set; } = 1;

        public bool RemoveExistingTBD { get; set; } = false;

        public bool UpdateWindowPositionType { get; set; } = false;

        public WorkflowSettings()
        {

        }

        public WorkflowSettings(JsonObject jObject)
        {
            FromJsonObject(jObject);
        }

        public WorkflowSettings(WorkflowSettings workflowSettings)
        {
            if(workflowSettings != null)
            {
                Path_TBD = workflowSettings.Path_TBD;
                Path_gbXML = workflowSettings.Path_gbXML;
                Path_TBD_Canonical = workflowSettings.Path_TBD_Canonical;
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
                RemoveIZAMs = workflowSettings.RemoveIZAMs;
                RemoveMechanicalVentilationGains = workflowSettings.RemoveMechanicalVentilationGains;
                SimulateFrom = workflowSettings.SimulateFrom;
                SimulateTo = workflowSettings.SimulateTo;

                RemoveExistingTBD = workflowSettings.RemoveExistingTBD;

                UpdateWindowPositionType = workflowSettings.UpdateWindowPositionType;
            }
        }

        public bool FromJsonObject(JsonObject jObject)
        {
            if(jObject == null)
            {
                return false;
            }

            if(jObject.ContainsKey("Path_TBD"))
            {
                Path_TBD = jObject["Path_TBD"]?.GetValue<string>() ?? null;
            }

            if (jObject.ContainsKey("Path_TBD_Canonical"))
            {
                Path_TBD_Canonical = jObject["Path_TBD_Canonical"]?.GetValue<string>() ?? null;
            }

            if (jObject.ContainsKey("Path_gbXML"))
            {
                Path_gbXML = jObject["Path_gbXML"]?.GetValue<string>() ?? null;
            }

            if (jObject.ContainsKey("WeatherData"))
            {
                WeatherData = new Weather.WeatherData(jObject["WeatherData"] as JsonObject);
            }

            if (jObject.ContainsKey("DesignDays_Heating"))
            {
                JsonArray jArray = jObject["DesignDays_Heating"] as JsonArray;
                if(jArray != null)
                {
                    DesignDays_Heating = new List<DesignDay>();
                    foreach(JsonNode jsonNode_DesignDay in jArray)
                    {
                        if (!(jsonNode_DesignDay is JsonObject jObject_DesignDay))
                        {
                            continue;
                        }

                        DesignDays_Heating.Add(new DesignDay(jObject_DesignDay));
                    }
                }
            }

            if (jObject.ContainsKey("DesignDays_Cooling"))
            {
                JsonArray jArray = jObject["DesignDays_Cooling"] as JsonArray;
                if (jArray != null)
                {
                    DesignDays_Cooling = new List<DesignDay>();
                    foreach (JsonNode jsonNode_DesignDay in jArray)
                    {
                        if (!(jsonNode_DesignDay is JsonObject jObject_DesignDay))
                        {
                            continue;
                        }

                        DesignDays_Cooling.Add(new DesignDay(jObject_DesignDay));
                    }
                }
            }

            if (jObject.ContainsKey("SurfaceOutputSpecs"))
            {
                JsonArray jArray = jObject["SurfaceOutputSpecs"] as JsonArray;
                if (jArray != null)
                {
                    SurfaceOutputSpecs = new List<SurfaceOutputSpec>();
                    foreach (JsonNode jsonNode_SurfaceOutputSpec in jArray)
                    {
                        if (!(jsonNode_SurfaceOutputSpec is JsonObject jObject_SurfaceOutputSpec))
                        {
                            continue;
                        }

                        SurfaceOutputSpecs.Add(new SurfaceOutputSpec(jObject_SurfaceOutputSpec));
                    }
                }
            }


            if (jObject.ContainsKey("UnmetHours"))
            {
                UnmetHours = jObject["UnmetHours"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("Simulate"))
            {
                Simulate = jObject["Simulate"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("Sizing"))
            {
                Sizing = jObject["Sizing"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("UpdateZones"))
            {
                UpdateZones = jObject["UpdateZones"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("UseWidths"))
            {
                UseWidths = jObject["UseWidths"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("AddIZAMs"))
            {
                AddIZAMs = jObject["AddIZAMs"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("RemoveIZAMs"))
            {
                RemoveIZAMs = jObject["RemoveIZAMs"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("RemoveMechanicalVentilationGains"))
            {
                RemoveMechanicalVentilationGains = jObject["RemoveMechanicalVentilationGains"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("SimulateFrom"))
            {
                SimulateFrom = jObject["SimulateFrom"]?.GetValue<int>() ?? default(int);
            }

            if (jObject.ContainsKey("SimulateTo"))
            {
                SimulateTo = jObject["SimulateTo"]?.GetValue<int>() ?? default(int);
            }

            if (jObject.ContainsKey("RemoveExistingTBD"))
            {
                RemoveExistingTBD = jObject["RemoveExistingTBD"]?.GetValue<bool>() ?? default(bool);
            }

            if (jObject.ContainsKey("UpdateWindowPositionType"))
            {
                UpdateWindowPositionType = jObject["UpdateWindowPositionType"]?.GetValue<bool>() ?? default(bool);
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));
            
            if(Path_TBD != null)
            {
                jObject.Add("Path_TBD", Path_TBD);
            }

            if (Path_TBD_Canonical != null)
            {
                jObject.Add("Path_TBD_Canonical", Path_TBD_Canonical);
            }

            if (Path_gbXML != null)
            {
                jObject.Add("Path_gbXML", Path_gbXML);
            }

            if (WeatherData != null)
            {
                jObject.Add("WeatherData", WeatherData.ToJsonObject());
            }

            if (DesignDays_Heating != null)
            {
                JsonArray jArray = new JsonArray();
                foreach(DesignDay designDay in DesignDays_Heating)
                {
                    jArray.Add(designDay.ToJsonObject());
                }

                jObject.Add("DesignDays_Heating", jArray);
            }

            if (DesignDays_Cooling != null)
            {
                JsonArray jArray = new JsonArray();
                foreach (DesignDay designDay in DesignDays_Cooling)
                {
                    jArray.Add(designDay.ToJsonObject());
                }

                jObject.Add("DesignDays_Cooling", jArray);
            }

            if (SurfaceOutputSpecs != null)
            {
                JsonArray jArray = new JsonArray();
                foreach (SurfaceOutputSpec surfaceOutputSpec in SurfaceOutputSpecs)
                {
                    jArray.Add(surfaceOutputSpec.ToJsonObject());
                }

                jObject.Add("SurfaceOutputSpecs", jArray);
            }

            jObject.Add("UnmetHours", UnmetHours);
            jObject.Add("Simulate", Simulate);
            jObject.Add("Sizing", Sizing);
            jObject.Add("UpdateZones", UpdateZones);
            jObject.Add("UseWidths", UseWidths);
            jObject.Add("AddIZAMs", AddIZAMs);
            jObject.Add("RemoveIZAMs", RemoveIZAMs);
            jObject.Add("RemoveMechanicalVentilationGains", RemoveMechanicalVentilationGains);

            jObject.Add("SimulateFrom", SimulateFrom);
            jObject.Add("SimulateTo", SimulateTo);

            jObject.Add("RemoveExistingTBD", RemoveExistingTBD);

            jObject.Add("UpdateWindowPositionType", UpdateWindowPositionType);

            return jObject;
        }
    }
}
