// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Modify
    {
        /// <summary>
        /// Simulates a TPD and answers whether the run is <b>evidenced</b>, not merely whether it was
        /// attempted.
        /// <para>
        /// This used to return a literal <c>true</c>. It did so even when <c>EnergyCentre</c> was null and the
        /// whole simulation had been silently skipped: measured on real TAS, a document holding zero plant
        /// rooms, zero systems and zero zones answered <c>true</c>, having produced no result of any kind.
        /// </para>
        /// <para>
        /// A TPD writes its results back into <b>the same document</b>, so the document must never be deleted
        /// beforehand, and a changed file proves nothing - <c>Save()</c> writes whether or not anything was
        /// produced. That makes this shape's evidence necessarily incomplete here: the decisive stage is
        /// reading the results back and reconciling them against the expected room set, which only the caller
        /// holding that set can do. Use the <c>out SimulationEvidence</c> overload and call
        /// <c>RecordResultsReconciled</c> once that check passes; until then
        /// <c>SimulationEvidence.Completed</c> stays false.
        /// </para>
        /// </summary>
        public static bool Simulate(string path_TPD, int startHour, int endHour)
        {
            return Simulate(path_TPD, startHour, endHour, out SimulationEvidence simulationEvidence)
                && simulationEvidence != null;
        }

        /// <summary>
        /// Simulates a TPD in place and reports every stage of what it left behind.
        /// </summary>
        /// <param name="path_TPD">The document to simulate. It is written back into and is never deleted.</param>
        /// <param name="startHour">0-based first hour. TAS is 1-based, so <c>+1</c> is applied on the way in.</param>
        /// <param name="endHour">0-based last hour, inclusive.</param>
        /// <param name="simulationEvidence">
        /// What the run left behind. Never null. <c>Completed</c> stays false until the caller has reconciled
        /// the results, because for this shape a saved file is not evidence.
        /// </param>
        /// <returns>
        /// True when every stage this method can judge passed. That is <b>not</b> the same as the simulation
        /// having produced usable results - see the remarks on the other overload.
        /// </returns>
        /// <summary>
        /// Simulates only the <b>air systems</b> in a TPD, rather than the whole plant room.
        /// <para>
        /// <b>Why this exists, measured on licensed TAS.</b> The Part O Iteration 3 route is ventilation
        /// only: what it needs out of TAS Systems is each zone's <c>ZoneTemperature</c>. Simulating the
        /// whole document also simulates the <i>plant</i> side - and on the shipped <c>MV.json</c>
        /// template that plant (multi-boiler, multi-chiller, an air-source heat pump and a DHW circuit
        /// whose junctions are left dangling) fails to size, so TAS answers <c>"Sizing Flow Failed"</c>
        /// and no results appear.
        /// </para>
        /// <para>
        /// The air system itself is perfectly valid. Measured on exactly that document:
        /// </para>
        /// <code>
        /// ISystem.Simulate     -> "Done"                  and 24 finite ZoneTemperature values
        /// IPlantRoom.Simulate  -> "Sizing Flow Failed"
        /// IPlantRoom.SimulateExx -> "Sizing Flow Failed"  (resetSizes 0 and 1 alike)
        /// ITPD.Simulate        -> "Sizing Flow Failed"
        /// </code>
        /// <para>
        /// So the route simulates what it actually needs. The document-level overload is unchanged for
        /// callers that do want plant results.
        /// </para>
        /// <para>
        /// <b>Which plant property, exactly.</b> Measured again after the foundation was frozen: TAS
        /// cannot derive a design flow into <c>Multi Boiler 1</c>, the DHW circuit's boiler, because the
        /// shipped template states a design flow delta-T of <b>zero</b> on both that boiler and its
        /// <c>DHW Circuit Group</c>. Any non-zero delta-T on either makes the same document answer
        /// <c>"Done"</c>; the multi-boiler's DHW duty flag changes nothing either way. It is template
        /// data, not a conversion defect - this converts the zero faithfully - and it cannot reach a
        /// Candidate B number, which was verified by comparing every room's <c>ZoneTemperature</c> with
        /// and without the correction. See
        /// the SAM_Tas PR record.
        /// </para>
        /// </summary>
        /// <param name="simulationEvidence">What the run left behind. Never null.</param>
        public static bool SimulateSystems(string path_TPD, int startHour, int endHour, out SimulationEvidence simulationEvidence)
        {
            return SimulateSystems(path_TPD, null, startHour, endHour, out simulationEvidence, out _);
        }

        /// <summary>
        /// Simulates every air system and, when room bindings are supplied, captures that system's
        /// zone-temperature results before simulating the next one.
        /// <para>
        /// Licensed TAS replaces the previous air system's in-memory result surface when
        /// <c>ISystem.Simulate</c> is called for another system in the same document. Reading all zones
        /// only after the loop therefore loses every system except the last. The result arrays copied
        /// here are ordinary managed values, so they remain valid after the next native call.
        /// </para>
        /// </summary>
        public static bool SimulateSystems(
            string path_TPD,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            int startHour,
            int endHour,
            out SimulationEvidence simulationEvidence,
            out SystemZoneTemperatureResults systemZoneTemperatureResults)
        {
            return SimulateSystems(path_TPD, systemVentilationBindings, startHour, endHour, out simulationEvidence, out systemZoneTemperatureResults, null);
        }

        /// <summary>
        /// As above, reporting each air system's <c>ISystem.Simulate</c> to <paramref name="progress"/> as
        /// <see cref="SystemVentilationRouteStage.SimulatingAirSystems"/> with a real count. Null reports nothing.
        /// </summary>
        public static bool SimulateSystems(
            string path_TPD,
            IEnumerable<SystemVentilationBinding> systemVentilationBindings,
            int startHour,
            int endHour,
            out SimulationEvidence simulationEvidence,
            out SystemZoneTemperatureResults systemZoneTemperatureResults,
            Action<SystemVentilationRouteProgress> progress)
        {
            simulationEvidence = new SimulationEvidence(SimulationOutputShape.InPlaceDocument, path_TPD, null);
            systemZoneTemperatureResults = systemVentilationBindings == null
                ? null
                : new SystemZoneTemperatureResults(startHour, endHour);

            Dictionary<string, List<SystemVentilationBinding>> bindings_By_System = null;
            if (systemVentilationBindings != null)
            {
                bindings_By_System = new Dictionary<string, List<SystemVentilationBinding>>(StringComparer.OrdinalIgnoreCase);
                foreach (SystemVentilationBinding binding in systemVentilationBindings)
                {
                    if (binding == null || string.IsNullOrWhiteSpace(binding.Reference_System))
                    {
                        continue;
                    }

                    if (!bindings_By_System.TryGetValue(binding.Reference_System, out List<SystemVentilationBinding> bindings))
                    {
                        bindings = new List<SystemVentilationBinding>();
                        bindings_By_System[binding.Reference_System] = bindings;
                    }

                    bindings.Add(binding);
                }
            }

            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                simulationEvidence.Refuse("No TPD path was given.");
                return false;
            }

            if (!File.Exists(path_TPD))
            {
                simulationEvidence.Refuse(string.Concat("The TPD does not exist: ", path_TPD));
                return false;
            }

            if (endHour < startHour)
            {
                simulationEvidence.Refuse(
                    string.Format("The requested period ends before it starts: {0}..{1}.", startHour, endHour));
                return false;
            }

            simulationEvidence.Prepare(DateTime.UtcNow);

            if (simulationEvidence.Refusals.Count != 0)
            {
                return false;
            }

            IDisposable measure_Open = TPDProfiler.Current?.Measure("Simulate: open TPD");

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                measure_Open?.Dispose();

                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;

                if (tPDDoc == null)
                {
                    simulationEvidence.RecordCallFailed(string.Concat("the document could not be opened: ", path_TPD));
                    return false;
                }

                EnergyCentre energyCentre = tPDDoc.EnergyCentre;

                if (energyCentre == null)
                {
                    simulationEvidence.RecordCallFailed(
                        string.Concat("the document carries no energy centre, so there is nothing to simulate: ", path_TPD));
                    return false;
                }

                int count_Simulated = 0;
                string diagnostic_Last = null;

                //Known before the first call, so "n of m" is a real count. Only asked for when someone listens.
                int count_System_Total = 0;
                int count_System_Started = 0;
                if (progress != null)
                {
                    try
                    {
                        int count_PlantRoom_Total = energyCentre.GetPlantRoomCount();
                        for (int i = 1; i <= count_PlantRoom_Total; i++)
                        {
                            count_System_Total += energyCentre.GetPlantRoom(i)?.GetSystemCount() ?? 0;
                        }
                    }
                    catch (Exception)
                    {
                        count_System_Total = 0;
                    }
                }

                // Every air system's answer, kept separately. With more than one system the document
                // has more than one diagnostic, and reporting only the last would hide the others.
                List<string> diagnostics = new List<string>();

                try
                {
                    int count_PlantRoom = energyCentre.GetPlantRoomCount();

                    for (int i = 1; i <= count_PlantRoom; i++)
                    {
                        PlantRoom plantRoom = energyCentre.GetPlantRoom(i);
                        if (plantRoom == null)
                        {
                            continue;
                        }

                        int count_System = plantRoom.GetSystemCount();

                        for (int j = 1; j <= count_System; j++)
                        {
                            global::TPD.System system = plantRoom.GetSystem(j);
                            if (system == null)
                            {
                                continue;
                            }

                            count_System_Started++;
                            SystemVentilationConversionContext.ReportProgress(progress, SystemVentilationRouteStage.SimulatingAirSystems, count_System_Started, count_System_Total);

                            // ISystem.Simulate returns a diagnostic string, exactly like the document-level
                            // call. "Done" is the measured success answer.
                            using (TPDProfiler.Current?.Measure("Simulate: TAS ISystem.Simulate (per air system)"))
                            {
                                diagnostic_Last = system.Simulate(startHour + 1, endHour + 1, 0);
                            }
                            count_Simulated++;
                            diagnostics.Add(diagnostic_Last);

                            if (SimulationDiagnostic.IsFailure(diagnostic_Last))
                            {
                                simulationEvidence.RecordCallReturned(diagnostic_Last);
                                return false;
                            }

                            if (bindings_By_System != null)
                            {
                                string reference_System = Query.NativeReference(system);
                                if (reference_System != null
                                    && bindings_By_System.TryGetValue(reference_System, out List<SystemVentilationBinding> bindings))
                                {
                                    //The per-SYSTEM overload: passing the system that has just been
                                    //simulated keeps this linear in the rooms it serves. Going through
                                    //the document would re-index every air system once per system,
                                    //which is quadratic in the unit count.
                                    SystemZoneTemperatureResults captured;
                                    using (TPDProfiler.Current?.Measure("Simulate: read ZoneTemperature (per air system)"))
                                    {
                                        captured = Convert.ToSAM_SystemZoneTemperatureResults(
                                            system,
                                            bindings,
                                            startHour,
                                            endHour);
                                    }

                                    foreach (SystemZoneTemperatureResult result in captured.Results)
                                    {
                                        systemZoneTemperatureResults.Add(result);
                                    }
                                }
                            }
                        }
                    }

                    using (TPDProfiler.Current?.Measure("Simulate: save TPD"))
                    {
                        tPDDoc.Save();
                    }
                }
                catch (Exception exception)
                {
                    simulationEvidence.RecordCallFailed(
                        string.Format("{0}: {1}", exception.GetType().Name, exception.Message));
                    return false;
                }

                if (count_Simulated == 0)
                {
                    simulationEvidence.RecordCallFailed("the document carries no air system, so nothing was simulated.");
                    return false;
                }

                simulationEvidence.Note(string.Format("{0} air system(s) simulated.", count_Simulated));

                foreach (string diagnostic in diagnostics)
                {
                    simulationEvidence.Note(string.Format("An air system answered: \"{0}\".", (diagnostic ?? string.Empty).Trim()));
                }

                // With several air systems there are several answers. Reporting one of them as THE
                // diagnostic would be a choice the measurement does not support, so a set of answers
                // that are not all the same is preserved verbatim and classified conservatively - the
                // combined text matches no measured success answer whole, so it decides nothing and
                // the zone temperature reconciliation remains the gate. A single distinct answer -
                // which is every case measured so far - is reported exactly as TAS gave it.
                string diagnostic_Reported = Distinct(diagnostics);

                if (!simulationEvidence.RecordCallReturned(diagnostic_Reported))
                {
                    return false;
                }
            }

            simulationEvidence.Conclude();

            return simulationEvidence.Refusals.Count == 0;
        }

        /// <summary>
        /// The one answer to report for a set of per-system answers: that answer when they all agree,
        /// and all of them joined when they do not - which classifies as unrecognised, decides nothing,
        /// and loses no text.
        /// </summary>
        private static string Distinct(List<string> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0)
            {
                return null;
            }

            string first = (diagnostics[0] ?? string.Empty).Trim();

            for (int i = 1; i < diagnostics.Count; i++)
            {
                if (!string.Equals((diagnostics[i] ?? string.Empty).Trim(), first, StringComparison.Ordinal))
                {
                    return string.Join(" | ", diagnostics.ConvertAll(x => (x ?? string.Empty).Trim()));
                }
            }

            return first;
        }

        public static bool Simulate(string path_TPD, int startHour, int endHour, out SimulationEvidence simulationEvidence)
        {
            simulationEvidence = new SimulationEvidence(SimulationOutputShape.InPlaceDocument, path_TPD, null);

            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                simulationEvidence.Refuse("No TPD path was given.");
                return false;
            }

            if (!File.Exists(path_TPD))
            {
                simulationEvidence.Refuse(string.Concat("The TPD does not exist: ", path_TPD));
                return false;
            }

            if (endHour < startHour)
            {
                simulationEvidence.Refuse(
                    string.Format("The requested period ends before it starts: {0}..{1}.", startHour, endHour));
                return false;
            }

            simulationEvidence.Prepare(DateTime.UtcNow);

            if (simulationEvidence.Refusals.Count != 0)
            {
                // Preparation refused - a stale error log that could be neither cleared nor dated, say.
                return false;
            }

            using (SAMTPDDocument sAMTPDDocument = new SAMTPDDocument(path_TPD))
            {
                TPDDoc tPDDoc = sAMTPDDocument.TPDDocument;

                if (tPDDoc == null)
                {
                    simulationEvidence.RecordCallFailed(string.Concat("the document could not be opened: ", path_TPD));
                    return false;
                }

                if (tPDDoc.EnergyCentre == null)
                {
                    // The silent-skip path. It used to fall through to `return true`.
                    simulationEvidence.RecordCallFailed(
                        string.Concat("the document carries no energy centre, so there is nothing to simulate: ", path_TPD));
                    return false;
                }

                try
                {
                    // ITPD.Simulate is declared as returning a STRING, and this call used to discard it.
                    // Measured on licensed TAS, that string diagnoses why nothing ran - "Plant room has no
                    // components", "Plant Room Has Errors", "Failed to open the TSD file", "Sizing Flow
                    // Failed". It is preserved verbatim as evidence.
                    //
                    // It is NOT treated as "non-empty means failure": no successful run has been observed
                    // through this route, so TAS may return a status on success too. Only a measured
                    // failure refuses here; anything else is recorded and left to the results
                    // reconciliation, which is the decisive gate.
                    string returned = tPDDoc.Simulate(startHour + 1, endHour + 1, 0);

                    tPDDoc.Save();

                    if (!simulationEvidence.RecordCallReturned(returned))
                    {
                        return false;
                    }
                }
                catch (Exception exception)
                {
                    simulationEvidence.RecordCallFailed(
                        string.Format("{0}: {1}", exception.GetType().Name, exception.Message));
                    return false;
                }
            }

            simulationEvidence.Conclude();

            return simulationEvidence.Refusals.Count == 0;
        }
    }
}
