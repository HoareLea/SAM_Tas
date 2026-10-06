// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Text.Json.Nodes;
using SAM.Core;
using SAM.Core.Tas;
using SAM.Weather;
using System.Collections.Generic;
using System.Diagnostics;
using TBD;

namespace SAM.Analytical.Tas
{
    public class WorkflowCalculator : IJSAMObject
    {
        public WorkflowCalculator()
        {

        }

        public WorkflowCalculator(JsonObject jObject)
        {
            FromJsonObject(jObject);
        }

        public WorkflowCalculator(WorkflowSettings workflowSettings)
        {
            WorkflowSettings = workflowSettings == null ? null : new WorkflowSettings(workflowSettings);
        }

        public event WorkflowCalculatorStartedEventHandler Ended;
        public event WorkflowCalculatorStartedEventHandler Started;
        public event WorkflowCalculatorStepsCountedEventHandler StepsCounted;
        public event WorkflowCalculatorUpdatingEventHandler Updating;

        private readonly Stopwatch stepStopwatch = new Stopwatch();
        private string currentStepName;
        private readonly List<KeyValuePair<string, double>> timings = new List<KeyValuePair<string, double>>();
        private readonly List<string> notes = new List<string>();

        public IReadOnlyList<KeyValuePair<string, double>> Timings => timings;

        /// <summary>
        /// What the workflow wants the caller to know, one sentence each - refusals, skips and
        /// identifications the previous implementation dropped silently. The aperture-type step in
        /// particular reports, per TBD aperture, which SAM aperture it matched (or which gate stopped it)
        /// and any schedule refusal, which is what makes a Part O schedule that failed to reach the TBD
        /// diagnosable instead of invisible. Callers surface this from Grasshopper (runtime warnings) or
        /// their own progress UI.
        /// </summary>
        public IReadOnlyList<string> Notes => notes;

        public WorkflowSettings WorkflowSettings { get; set; }

        /// <summary>
        /// Optional cooperative cancellation. Defaults to <see cref="System.Threading.CancellationToken.None"/>
        /// so existing callers (e.g. the benchmark CLI) are unaffected. It is observed once per stage in
        /// <see cref="Step"/>, so a cancel aborts before the next step begins; it does NOT interrupt the
        /// in-flight, uninterruptible TAS COM simulate/sizing call within a step.
        /// </summary>
        public System.Threading.CancellationToken CancellationToken { get; set; } = System.Threading.CancellationToken.None;

        private void Step(string description)
        {
            // Finalize first so the stage that just completed keeps its timing.
            FinalizeCurrentStep();

            // Already cancelled: stop without announcing a stage that will not run.
            CancellationToken.ThrowIfCancellationRequested();

            // Raise Updating BEFORE deciding to proceed. A WinForms listener pumps the message queue here
            // (ProgressForm.Update -> Application.DoEvents), so this is where a queued Cancel click is actually
            // delivered. Checking the token only before this call would observe a stale state and let this
            // stage - potentially an expensive or file-writing one - start anyway.
            Updating?.Invoke(this, new WorkflowCalculatorUpdatingEventArgs(description));

            // Observe a cancel delivered by that pump, before this stage does any work. Cancelling can still
            // leave partially written .t3d/.tbd/.tsd files behind - a later clean rerun should set
            // _removeTBD_ true.
            CancellationToken.ThrowIfCancellationRequested();

            currentStepName = description;
            stepStopwatch.Restart();
        }

        /// <summary>
        /// The second cancellation choke point, observed before every normal return from
        /// <see cref="Calculate"/>. <see cref="Step"/> only covers cancels seen before a stage starts, so a
        /// Cancel clicked during a terminal stage - "Saving Model", or the last stage on the no-weather and
        /// no-simulation paths - would otherwise be recorded by the progress dialog and never seen here,
        /// letting Calculate return normally and the caller report success.
        /// </summary>
        private void ThrowIfCancelledBeforeReturning()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }

        private void FinalizeCurrentStep()
        {
            if (currentStepName == null)
                return;

            stepStopwatch.Stop();
            timings.Add(new KeyValuePair<string, double>(currentStepName, stepStopwatch.Elapsed.TotalMilliseconds));
            currentStepName = null;
        }

        private void WriteTimingsCsv(string directory, string fileName)
        {
            FinalizeCurrentStep();
            if (timings.Count == 0 || string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName))
                return;

            try
            {
                string path = System.IO.Path.Combine(directory, fileName + ".timing.csv");
                List<string> lines = new List<string> { "step,milliseconds" };
                double total = 0.0;
                foreach (KeyValuePair<string, double> entry in timings)
                {
                    string safeName = entry.Key?.Replace("\"", "\"\"") ?? string.Empty;
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "\"{0}\",{1:F1}", safeName, entry.Value));
                    total += entry.Value;
                }
                lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "\"TOTAL\",{0:F1}", total));
                System.IO.File.WriteAllLines(path, lines);
            }
            catch
            {
                // Timing instrumentation must never break the workflow.
            }
        }
        
        /// <summary>
        /// Runs the workflow over a model this calculator does <b>not</b> own, and hands back what it
        /// produced. The caller's instance is untouched: a working copy is taken here.
        /// <para>
        /// This is the entry point for every caller that has not thought about ownership - the Grasshopper
        /// components, the benchmark CLI, anything reaching <c>Modify.RunWorkflow</c> without saying
        /// otherwise - and it is deliberately the safe one. See
        /// <see cref="Calculate(AnalyticalModel, bool)"/> for the caller that already owns its model.
        /// </para>
        /// </summary>
        public AnalyticalModel Calculate(AnalyticalModel analyticalModel)
        {
            return Calculate(analyticalModel, false);
        }

        /// <summary>
        /// Runs the workflow, stating whether the model handed over is already the caller's own working
        /// copy.
        ///
        /// <para><b>The ownership seam</b></para>
        /// <para>
        /// Everything below mutates the model IN PLACE - <see cref="Modify.UpdateIds"/> stamps
        /// <c>SpaceParameter.ZoneGuid</c>, <c>PanelParameter.ZoneSurfaceReference_1</c>/<c>_2</c>,
        /// <c>PanelParameter.BuildingElementGuid</c> and the aperture identity parameters straight onto the
        /// live objects' parameter sets, and <c>UpdateAdiabatic</c>, <c>UpdateBuildingElements</c>,
        /// <c>UpdateThermalParameters</c> and <c>UpdateApertureDefinitions</c> do the same. So a model the
        /// workflow is given has to be one somebody is entitled to mutate.
        /// </para>
        /// <para>
        /// <b>false</b> - the default, and every public caller - takes that working copy here, through
        /// <c>new AnalyticalModel(analyticalModel, true)</c>. That is what guarantees a failed or cancelled
        /// run leaves the caller's model exactly as it was.
        /// </para>
        /// <para>
        /// <b>true</b> says the caller has already taken one and this run may mutate it directly. It is a
        /// promise about the argument, not a request to skip protection: pass it only from a boundary that
        /// itself cloned, and whose clone no other holder shares. <c>Modify.RunPartOSimulation</c> is that
        /// boundary for Approved Document O - it clones once, at the point it renames and re-materials the
        /// model, and everything from there to here works on that one copy. Without this the normal Part O
        /// run cloned the whole model three times over for one isolation guarantee, which on a five
        /// thousand space project is most of a second and half a gigabyte of allocation to no end.
        /// </para>
        /// <para>
        /// Getting it wrong in the unsafe direction - passing true from a caller that did not clone - puts
        /// this run's TAS identities on somebody else's model, which is precisely the defect the copy
        /// exists to prevent. When in doubt, pass false: the cost is one clone.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The model to run the workflow over.</param>
        /// <param name="analyticalModel_Owned">
        /// True only where the caller has already taken a deep working copy that nothing else holds.
        /// </param>
        public AnalyticalModel Calculate(AnalyticalModel analyticalModel, bool analyticalModel_Owned)
        {
            //A calculator instance can be re-run; each run reports only its own notes. Cleared ahead of the
            //validation gate below, so a run rejected there leaves no previous run's notes visible either.
            notes.Clear();

            Started?.Invoke(this, new System.EventArgs());

            if (analyticalModel == null || WorkflowSettings == null)
            {
                return null;
            }

            // Before anything else: the setup below unconditionally deletes an existing .t3d, and the .tbd
            // too when RemoveExistingTBD is set, all of it ahead of the first Step. A caller handing in an
            // already-cancelled token (the WorkflowTBD pre-step shares one) must not lose those files.
            CancellationToken.ThrowIfCancellationRequested();

            // "Convert the geometry" and "the geometry is already converted" are contradictory
            // instructions, and choosing between them here would be this class deciding something the
            // caller has to. Refused BEFORE any file is touched.
            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_TBD_Canonical) && !string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
            {
                notes.Add("This run was given both a gbXML to convert and a canonical TBD to start from, which are contradictory instructions - one says the geometry must be converted, the other that it already is. Nothing was run. Supply one or the other.");

                Ended?.Invoke(this, new System.EventArgs());

                return null;
            }

            // THE working model, and the reason a copy has to be DEEP when this run takes one itself.
            //
            // Everything below works on `result` and the model is handed back at the end, so the intent has
            // always been that the caller's instance is untouched until it chooses to adopt what comes back.
            // The ordinary copy constructor does not deliver that: it rebuilds the cluster's dictionaries
            // but stores the SAME Space, Panel and Aperture instances, which is safe only for an operation
            // that writes by same-guid replacement. This one does not - see the summary on this method for
            // the list of steps that mutate in place. Against a shallow copy every one of those writes was
            // visible through the caller's model.
            //
            // Converting each of those steps to replacement semantics would be a redesign of the conversion
            // for an isolation guarantee one copy already gives, so the copy is where it is fixed. It is on
            // no read path: the shallow copy stays the default everywhere else precisely so a getter never
            // pays for this.
            //
            // Skipped where the caller already owns the model, which is the whole point of the overload -
            // otherwise the normal Approved Document O run took three copies of the same model to establish
            // one guarantee. The clone is then the caller's, taken once, and `result` mutates it directly.
            //
            // See AnalyticalModel(AnalyticalModel, bool) for the rule, and the Part O optimiser for what
            // depended on it: its caller is the retained last-valid design, and a round that stamped new
            // TAS identities onto that model left it disagreeing with its own persisted
            // SimulationResultProvenance after a later round failed or was cancelled.
            AnalyticalModel result = analyticalModel_Owned ? analyticalModel : new AnalyticalModel(analyticalModel, true);

            string directory = System.IO.Path.GetDirectoryName(WorkflowSettings.Path_TBD);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(WorkflowSettings.Path_TBD);

            string path_TSD = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "tsd"));

            analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.WeatherData, out WeatherData weatherData);
            if (WorkflowSettings?.WeatherData != null)
            {
                weatherData = WorkflowSettings.WeatherData;
            }

            // WorkflowSettings.WeatherData - not the resolved weatherData above - is what decides here: it is
            // the only case where the weather this run installs can DIFFER from the one the model's design
            // days were derived from. Reading the design days straight off the model would then size the
            // first TBD of the new weather on the previous weather's design days. See
            // Query.DesignDays_Authoritative for the full rule.
            Query.DesignDays_Authoritative(
                analyticalModel,
                WorkflowSettings?.WeatherData,
                WorkflowSettings?.DesignDays_Cooling,
                WorkflowSettings?.DesignDays_Heating,
                out List<DesignDay> coolingDesignDays,
                out List<DesignDay> heatingDesignDays);

            int count = 6;
            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
            {
                //Nine gbXML-only steps, plus "Reusing Aperture Definitions".
                count = count + 10;
            }

            //One step for the clone, so a warm-started run's progress reports what it actually does rather
            //than counting a conversion it is deliberately not performing.
            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_TBD_Canonical))
            {
                count++;
            }

            if (WorkflowSettings.UpdateZones)
            {
                count++;
            }

            if (WorkflowSettings.AddIZAMs)
            {
                count++;
            }

            if (WorkflowSettings.RemoveIZAMs)
            {
                count++;
            }

            if (WorkflowSettings.RemoveMechanicalVentilationGains)
            {
                count++;
            }

            //Count the step that actually runs: "Adding Design Days" is gated on the RESOLVED design days,
            //not on the settings, so a run whose design days came from the weather or the model counted one
            //step short of what it reported.
            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                count++;
            }

            if (WorkflowSettings.SurfaceOutputSpecs != null && WorkflowSettings.SurfaceOutputSpecs.Count > 0)
            {
                count = count + 2;
            }

            if (WorkflowSettings.Simulate)
            {
                count = count + 2;
            }

            if (WorkflowSettings.UnmetHours)
            {
                count++;
            }

            if (!WorkflowSettings.Sizing)
            {
                count--;
            }

            StepsCounted?.Invoke(this, new WorkflowCalculatorStepsCountedEventArgs(count));

            AdjacencyCluster adjacencyCluster = null;

            bool hasWeatherData = false;

            Core.Tas.Modify.SetProjectDirectory(directory);

            // ---- The warm start: this round's TBD is a COPY of the canonical one -------------------------
            //
            // A file copy, and deliberately nothing cleverer. The canonical TBD is opened only by the
            // framework's own copy, never by this process, so it cannot be mutated by a round however that
            // round then fails - which is what lets every round start from the same known state instead of
            // from its predecessor's leftovers.
            //
            // The conversion block below is skipped because a canonical TBD already carries every product
            // of it, and everything AFTER it still runs on the copy - see
            // WorkflowSettings.Path_TBD_Canonical.
            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_TBD_Canonical))
            {
                if (!System.IO.File.Exists(WorkflowSettings.Path_TBD_Canonical))
                {
                    notes.Add(string.Format("The canonical TBD '{0}' this run was to start from does not exist, so there is nothing to warm start from and nothing was run. Convert the model in full instead.", WorkflowSettings.Path_TBD_Canonical));

                    Ended?.Invoke(this, new System.EventArgs());

                    return null;
                }

                //Refused rather than resolved: a canonical path that IS the target would have the copy
                //below overwrite its own source, and every later round would then start from whatever the
                //last one left behind - the cumulative mutation the canonical baseline exists to prevent.
                if (string.Equals(System.IO.Path.GetFullPath(WorkflowSettings.Path_TBD_Canonical), System.IO.Path.GetFullPath(WorkflowSettings.Path_TBD), System.StringComparison.OrdinalIgnoreCase))
                {
                    notes.Add(string.Format("The canonical TBD and this run's TBD are the same file ('{0}'), so warm starting would write to the baseline every later run depends on. Nothing was run - give the run its own TBD path.", WorkflowSettings.Path_TBD));

                    Ended?.Invoke(this, new System.EventArgs());

                    return null;
                }

                Step("Copying Canonical TBD");

                try
                {
                    System.IO.File.Copy(WorkflowSettings.Path_TBD_Canonical, WorkflowSettings.Path_TBD, true);
                }
                catch (System.Exception exception)
                {
                    notes.Add(string.Format("The canonical TBD '{0}' could not be copied to '{1}', so this run has no TBD to work on and nothing was run: {2}", WorkflowSettings.Path_TBD_Canonical, WorkflowSettings.Path_TBD, exception.Message));

                    Ended?.Invoke(this, new System.EventArgs());

                    return null;
                }

                notes.Add(string.Format("This run started from the canonical TBD '{0}', copied to '{1}'. The geometry, constructions, apertures and shading calculation it carries were not recomputed; the ventilation state, the zone identities and the full-year simulation were.", WorkflowSettings.Path_TBD_Canonical, WorkflowSettings.Path_TBD));
            }

            if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
            {
                string path_T3D = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "t3d"));
                if (System.IO.File.Exists(path_T3D))
                {
                    System.IO.File.Delete(path_T3D);
                }

                if (WorkflowSettings.RemoveExistingTBD)
                {
                    if (System.IO.File.Exists(WorkflowSettings.Path_TBD))
                    {
                        System.IO.File.Delete(WorkflowSettings.Path_TBD);
                    }
                }

                // Only worth opening the T3D to read its GUID if a previous run left one on disk.
                // The unconditional delete above means this is currently always skipped (~8.8 s on a real model),
                // but the block stays in place so that making the delete conditional later restores GUID preservation for free.
                string guid = null;
                if (System.IO.File.Exists(path_T3D))
                {
                    Step("Extracting GUID");
                    using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
                    {
                        TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;
                        guid = t3DDocument.Building.GUID;
                        sAMT3DDocument.Save();
                    }
                }

                float latitude = float.NaN;
                float longitude = float.NaN;
                float timeZone = float.NaN;
                Step("Opening TBD file");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(WorkflowSettings.Path_TBD))
                {
                    TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                    Step("Updating Weather Data");
                    if (weatherData != null)
                    {
                        Weather.Tas.Modify.UpdateWeatherData(tBDDocument, weatherData, result.AdjacencyCluster.BuildingHeight());
                    }

                    if (!string.IsNullOrWhiteSpace(guid))
                    {
                        tBDDocument.Building.GUID = guid;
                    }

                    Step("Updating HDD and CDD Day Types");

                    Calendar calendar = tBDDocument.Building.GetCalendar();

                    List<dayType> dayTypes = Query.DayTypes(calendar);
                    if (dayTypes.Find(x => x.name == "HDD") == null)
                    {
                        dayType dayType = calendar.AddDayType();
                        dayType.name = "HDD";
                    }

                    if (dayTypes.Find(x => x.name == "CDD") == null)
                    {
                        dayType dayType = calendar.AddDayType();
                        dayType.name = "CDD";
                    }

                    latitude = tBDDocument.Building.latitude;
                    longitude = tBDDocument.Building.longitude;
                    timeZone = tBDDocument.Building.timeZone;

                    sAMTBDDocument.Save();
                }

                Step("Opening T3D file");
                using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
                {
                    TAS3D.T3DDocument t3DDocument = sAMT3DDocument.T3DDocument;

                    Step("Importing gbXML");
                    t3DDocument.TogbXML(WorkflowSettings.Path_gbXML, true, true, true);

                    //sets the window position to wall 2026.04.22



                    Step("Updating T3D file");
                    t3DDocument.SetUseBEWidths(WorkflowSettings.UseWidths);
                    result = Query.UpdateT3D(result, t3DDocument, WorkflowSettings.UpdateWindowPositionType);

                    t3DDocument.Building.latitude = float.IsNaN(latitude) ? t3DDocument.Building.latitude : latitude;
                    t3DDocument.Building.longitude = float.IsNaN(longitude) ? t3DDocument.Building.longitude : longitude;
                    t3DDocument.Building.timeZone = float.IsNaN(timeZone) ? t3DDocument.Building.timeZone : timeZone;

                    sAMT3DDocument.Save();

                    Step("T3D to TBD -> Shading");
                    Convert.ToTBD(t3DDocument, WorkflowSettings.Path_TBD, 1, 365, 15, true, false);
                }
            }

            Step("Opening TBD");
            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(WorkflowSettings.Path_TBD))
            {
                TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;

                hasWeatherData = tBDDocument?.Building.GetWeatherYear() != null;

                Step("Updating Facing External Elements");
                result = Query.UpdateFacingExternal(result, tBDDocument);

                Step("Assigning Adiabatic Constructions");
                Modify.AssignAdiabaticConstruction(tBDDocument, "Adiabatic", new string[] { "-unzoned", "-internal", "-exposed" }, false, true);

                Step("Setting Adiabatic");
                Modify.UpdateAdiabatic(tBDDocument, result, Tolerance.MacroDistance);

                Step("Updating Building Elements");
                Modify.UpdateBuildingElements(tBDDocument, result, out List<string> notes_BuildingElements);
                notes.AddRange(notes_BuildingElements);

                adjacencyCluster = result.AdjacencyCluster;
                Step("Updating Ids");
                Modify.UpdateIds(adjacencyCluster, tBDDocument.Building);

                if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
                {
                    // On this route TAS's own T3D -> TBD conversion created one aperture building element and
                    // one construction PER APERTURE PER PART, named after the aperture, because the gbXML
                    // opening name has to carry the aperture GUID for Query.UpdateT3D to decode. This step
                    // rebinds each physical surface onto the shared DEFINITION its aperture states - the same
                    // resolver the direct SAMAnalytical.TBD export runs - and sweeps up what TAS left over.
                    //
                    // It runs AFTER Updating Ids on purpose: that step has just stamped every aperture's
                    // physical surfaces and its current building element, which is exactly what the rebind
                    // reads instead of re-deriving either from geometry. The direct route needs none of this,
                    // so the step is gated on the gbXML route that does.
                    Step("Reusing Aperture Definitions");
                    Modify.UpdateApertureDefinitions(tBDDocument.Building, adjacencyCluster, result.MaterialLibrary, out List<string> notes_ApertureDefinitions);
                    notes.AddRange(notes_ApertureDefinitions);
                }

                Step("Updating Thermal Parameters");
                Modify.UpdateThermalParameters(adjacencyCluster, tBDDocument.Building);
                result = new AnalyticalModel(result, adjacencyCluster);

                if (WorkflowSettings.UpdateZones)
                {
                    Step("Updating Zones");
                    Modify.UpdateZones(tBDDocument.Building, result, true);
                }

                if (coolingDesignDays != null || heatingDesignDays != null)
                {
                    Step("Adding Design Days");
                    Modify.AddDesignDays(tBDDocument, coolingDesignDays, heatingDesignDays, 30);
                }

                Step("Creating Zone Groups");
                Modify.AddDefaultZoneGroups(tBDDocument?.Building, adjacencyCluster);

                if (!string.IsNullOrWhiteSpace(WorkflowSettings.Path_gbXML))
                {
                    Step("Updating Aperture Types");
                    Modify.SetApertureTypes(tBDDocument.Building, adjacencyCluster, out List<string> notes_ApertureTypes, Tolerance.MacroDistance);
                    notes.AddRange(notes_ApertureTypes);
                }

                if (WorkflowSettings.AddIZAMs)
                {
                    Step("Add IZAMs");
                    Modify.UpdateIZAMs(tBDDocument, adjacencyCluster);
                }

                //----------------------------------------------------------------------------------------
                //The no-IZAM thermal source. Both steps are default-off, so no existing caller moves.
                //
                //They run HERE, inside the document session that is already open, and not as a separate
                //pass: that adds no COM document cycle (SAMTBDDocument.Dispose tears down the shared Tas
                //session after a handful of them), it is after "Updating Zones" - which is what writes the
                //ticV profile in the first place - and it is before the save, the sizing and the
                //simulation, so what those see is the cleaned building.
                //----------------------------------------------------------------------------------------

                if (WorkflowSettings.RemoveIZAMs)
                {
                    Step("Removing IZAMs");

                    Modify.RemoveIZAMs(tBDDocument.Building);

                    //Verified, not trusted: the sweep walks GetIZAM(0)/RemoveIZAM(0) to exhaustion, so an
                    //IZAM surviving it means the building did not accept the removal. A survivor breaks the
                    //no-IZAM contract, so the run fails CLOSED - refusal, no save, no sizing, no
                    //simulation - rather than reporting success on a building that still carries its own
                    //mechanical ventilation. The decision is Query.IzamSurvivorRefusal; the return-null
                    //convention is the same one the other refusals above use, and NoIzamThermalSource
                    //already records a null return as a failed call, so the source cannot come back
                    //accepted. The refusal happens BEFORE the save below on purpose: the surviving-IZAM
                    //state is never persisted as this run's output.
                    string refusal_IzamSurvivor = Query.IzamSurvivorRefusal(true, tBDDocument.Building?.GetIZAM(0) != null);
                    if (refusal_IzamSurvivor != null)
                    {
                        notes.Add(refusal_IzamSurvivor);

                        Ended?.Invoke(this, new System.EventArgs());

                        return null;
                    }

                    notes.Add("Removing IZAMs: none remain.");
                }

                if (WorkflowSettings.RemoveMechanicalVentilationGains)
                {
                    Step("Removing Mechanical Ventilation Gains");

                    List<string> internalConditionNames = Modify.RemoveVentilationGains(tBDDocument, adjacencyCluster);

                    notes.Add(
                        string.Format(
                            "Removing Mechanical Ventilation Gains: ticV zeroed on {0} internal condition(s){1}. "
                            + "Infiltration (ticI) and natural ventilation (aperture types and opening schedules) "
                            + "are untouched - they are separate carriers.",
                            internalConditionNames == null ? 0 : internalConditionNames.Count,
                            internalConditionNames == null || internalConditionNames.Count == 0
                                ? string.Empty
                                : string.Concat(": ", string.Join(", ", internalConditionNames))));
                }

                sAMTBDDocument.Save();
            }

            if (coolingDesignDays != null || heatingDesignDays != null)
            {
                if (adjacencyCluster == null)
                {
                    adjacencyCluster = result.AdjacencyCluster;
                }

                if (adjacencyCluster != null)
                {
                    Modify.ReplaceDesignDays(adjacencyCluster, coolingDesignDays, heatingDesignDays);
                }
            }

            result = new AnalyticalModel(result, adjacencyCluster);

            if (!hasWeatherData)
            {
                WriteTimingsCsv(directory, fileName);
                ThrowIfCancelledBeforeReturning();
                return result;
            }

            if (WorkflowSettings.Sizing)
            {
                Step("Sizing");
                Query.Sizing(WorkflowSettings.Path_TBD, new SizingSettings() { ExcludeOutdoorAir = false, ExcludePositiveInternalGains = true }, result);
            }

            bool hasSurfaceOutputSpecs = WorkflowSettings.SurfaceOutputSpecs != null && WorkflowSettings.SurfaceOutputSpecs.Count > 0;

            if (hasSurfaceOutputSpecs && !WorkflowSettings.Simulate)
            {
                Step("Opening TBD");
                using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(WorkflowSettings.Path_TBD))
                {
                    TBDDocument tBDDocument = sAMTBDDocument.TBDDocument;
                    Step("Updating Surface Output Specs");
                    Core.Tas.Modify.UpdateSurfaceOutputSpecs(tBDDocument, WorkflowSettings.SurfaceOutputSpecs);
                    Step("Assigning Surface Output Specs");
                    Core.Tas.Modify.AssignSurfaceOutputSpecs(tBDDocument, WorkflowSettings.SurfaceOutputSpecs[0].Name);
                    sAMTBDDocument.Save();
                }
            }

            if (WorkflowSettings.Simulate)
            {
                Step("Simulating Model");
                Modify.Simulate(WorkflowSettings.Path_TBD, path_TSD, WorkflowSettings.SimulateFrom, WorkflowSettings.SimulateTo, SizingType.Undefined, hasSurfaceOutputSpecs ? WorkflowSettings.SurfaceOutputSpecs : null);
            }

            if (!WorkflowSettings.Simulate)
            {
                WriteTimingsCsv(directory, fileName);
                ThrowIfCancelledBeforeReturning();
                return result;
            }

            Step("Adding Results");
            adjacencyCluster = result.AdjacencyCluster;
            List<Result> results = Modify.AddResults(path_TSD, adjacencyCluster);

            if (WorkflowSettings.UnmetHours)
            {
                Step("Calculating Unmet Hours");
                List<Result> results_UnmetHours = Query.UnmetHours(path_TSD, WorkflowSettings.Path_TBD, 0.5);
                if (results_UnmetHours != null && results_UnmetHours.Count > 0)
                {
                    foreach (Result result_UnmetHours in results_UnmetHours)
                    {
                        if (result_UnmetHours is AdjacencyClusterSimulationResult)
                        {
                            adjacencyCluster.AddObject(result_UnmetHours);
                        }
                        else if (result_UnmetHours is SpaceSimulationResult)
                        {
                            SpaceSimulationResult spaceSimulationResult = (SpaceSimulationResult)result_UnmetHours;

                            List<SpaceSimulationResult> spaceSimulationResults = Query.Results(results, spaceSimulationResult);
                            if (spaceSimulationResults == null)
                                results.Add(spaceSimulationResult);
                            else
                                spaceSimulationResults.ForEach(x => Core.Modify.Copy(spaceSimulationResult, x, Analytical.SpaceSimulationResultParameter.UnmetHourFirstIndex, Analytical.SpaceSimulationResultParameter.UnmetHours, Analytical.SpaceSimulationResultParameter.OccupiedUnmetHours));
                        }
                    }
                }
            }

            Step("Updating Design Loads");
            adjacencyCluster = Modify.UpdateDesignLoads(WorkflowSettings.Path_TBD, adjacencyCluster);

            result = new AnalyticalModel(result, adjacencyCluster);

            if(result != null)
            {
                Step("Saving Model");
                string path_Json = System.IO.Path.Combine(directory, string.Format("{0}.{1}", fileName, "json"));

                Core.Convert.ToFile(result, path_Json, SAMFileType.Json);
            }

            WriteTimingsCsv(directory, fileName);

            ThrowIfCancelledBeforeReturning();

            Ended?.Invoke(this, new System.EventArgs());

            return result;
        }

        public bool FromJsonObject(JsonObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("WorkflowSettings"))
            {
                WorkflowSettings = new WorkflowSettings(jObject["WorkflowSettings"] as JsonObject);
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject jObject = new JsonObject();
            jObject.Add("_type", Core.Query.FullTypeName(this));

            if (jObject.ContainsKey("WorkflowSettings"))
            {
                WorkflowSettings = new WorkflowSettings(jObject["WorkflowSettings"] as JsonObject);
            }

            return jObject;
        }
    }
}
