// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core.Tas;
using System;
using System.Collections.Generic;
using System.IO;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Create
    {
        /// <summary>
        /// Runs the whole explicit Part O ventilation route against an already-built no-IZAM thermal
        /// source, and answers one coherent <see cref="SystemVentilationRoute"/>.
        /// <para>The stages, and why each is where it is:</para>
        /// <list type="number">
        /// <item><description><b>The intent.</b> PR1's lineage plus the thermal source's room-to-TAS-zone
        /// map become an explicit statement of what TAS is to build - every room, every leg, every duty,
        /// by identity.</description></item>
        /// <item><description><b>The duty carriers.</b> A <b>working copy</b> of PR1's graph gains one
        /// damper per extract and transfer leg, because a duct carries no design flow and a junction no
        /// members. PR1's own graph is never touched.</description></item>
        /// <item><description><b>The conversion</b>, which reconciles itself against the intent and
        /// refuses on any disagreement.</description></item>
        /// <item><description><b>The simulation</b>, per air system. <c>ISystem.Simulate</c>, not the
        /// document-level call: measured on licensed TAS, simulating the document also simulates the
        /// plant, and the shipped template's plant answers <c>"Sizing Flow Failed"</c> and produces
        /// nothing while the ventilation network beside it is perfectly
        /// valid.</description></item>
        /// <item><description><b>The results</b>, one <c>ZoneTemperature</c> series per room, resolved
        /// through the binding rather than found, and complete or refused.</description></item>
        /// </list>
        /// <para>
        /// <b>The path guard.</b> The TPD may not be written over the thermal source's own TBD or TSD -
        /// the conversion deletes its output first, and a mistyped path would destroy the building the
        /// route is standing on, silently, before anything had a chance to fail.
        /// </para>
        /// </summary>
        /// <param name="noIzamThermalSource">The simulated no-IZAM building and its room identity map.</param>
        /// <param name="mechanicalVentilationMaterialisation">PR1's materialised graph and lineage.</param>
        /// <param name="path_TPD">Where to write the TAS Systems document.</param>
        /// <param name="startHour">0-based first hour of the Systems simulation.</param>
        /// <param name="endHour">0-based last hour, inclusive.</param>
        /// <param name="fanHeatGainPolicy">
        /// PR5A (SAM#111 plan §D/§K.3): what the route does with a fan's native <c>HeatGainFactor</c>.
        /// <c>ClearToZero</c>, the B0 control, by default - a caller states <c>FromSystemsGraph</c> only
        /// for a manufacturer-aware run, where SAM_Systems has already resolved the figure onto the graph.
        /// </param>
        public static SystemVentilationRoute SystemVentilationRoute(
            NoIzamThermalSource noIzamThermalSource,
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation,
            string path_TPD,
            int startHour,
            int endHour,
            SystemVentilationFanHeatGainPolicy fanHeatGainPolicy = SystemVentilationFanHeatGainPolicy.ClearToZero)
        {
            //Where the route's time goes, beside the TPD as <name>.route.timing.csv: the conversion (itself
            //broken down in <name>.timing.csv), each TAS call and each read. Observation only.
            TPDProfiler profiler = new TPDProfiler();
            IDisposable activation = profiler.Activate();

            try
            {
                return SystemVentilationRoute(noIzamThermalSource, mechanicalVentilationMaterialisation, path_TPD, startHour, endHour, fanHeatGainPolicy, profiler);
            }
            finally
            {
                activation.Dispose();
                profiler.WriteCsv(path_TPD, ".route.timing.csv");
            }
        }

        private static SystemVentilationRoute SystemVentilationRoute(
            NoIzamThermalSource noIzamThermalSource,
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation,
            string path_TPD,
            int startHour,
            int endHour,
            SystemVentilationFanHeatGainPolicy fanHeatGainPolicy,
            TPDProfiler profiler)
        {
            profiler.Step("Route: guard and intent");

            List<string> refusals = new List<string>();
            List<string> notes = new List<string>();

            if (!TryGuard(noIzamThermalSource, mechanicalVentilationMaterialisation, path_TPD, startHour, endHour, refusals))
            {
                return new SystemVentilationRoute(noIzamThermalSource, path_TPD, null, null, null, null, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //1. The intent.
            //-------------------------------------------------------------------------------------------
            Core.Systems.SystemEnergyCentre systemEnergyCentre = mechanicalVentilationMaterialisation.SystemEnergyCentre;

            SystemVentilationConversionContext systemVentilationConversionContext = SystemVentilationConversionContext(
                systemEnergyCentre,
                mechanicalVentilationMaterialisation.Bindings,
                noIzamThermalSource.ZoneReferences,
                fanHeatGainPolicy,
                mechanicalVentilationMaterialisation.RecirculationCoolings,
                mechanicalVentilationMaterialisation.GuidanceCoolings);

            //-------------------------------------------------------------------------------------------
            //2. The duty carriers, in a working copy. PR1's graph is an input and stays one: the caller
            //   still holds it, and a route that mutated it would leave the design carrying PR2's
            //   plumbing.
            //-------------------------------------------------------------------------------------------
            profiler.Step("Route: duty carriers (working copy)");

            Core.Systems.SystemEnergyCentre systemEnergyCentre_Working = new Core.Systems.SystemEnergyCentre(systemEnergyCentre);

            Modify.MaterialiseVentilationDutyCarriers(systemEnergyCentre_Working, systemVentilationConversionContext);

            //-------------------------------------------------------------------------------------------
            //3. The conversion, which reconciles itself.
            //-------------------------------------------------------------------------------------------
            SystemEnergyCentreConversionSettings systemEnergyCentreConversionSettings = new SystemEnergyCentreConversionSettings
            {
                //The document-level Simulate is deliberately NOT used - see SimulateSystems below.
                Simulate = false,
                StartHour = startHour,
                EndHour = endHour,
                IncludeComponentResults = false,
                IncludeControllerResults = false,
            };

            bool converted;

            profiler.Step("Route: TPD generation (Convert.ToTPD - see .timing.csv)");

            try
            {
                converted = Convert.ToTPD(
                    systemEnergyCentre_Working,
                    path_TPD,
                    noIzamThermalSource.Path_TSD,
                    systemEnergyCentreConversionSettings,
                    systemVentilationConversionContext);
            }
            catch (Exception exception)
            {
                refusals.Add(string.Format(
                    "The conversion to TAS Systems threw {0}: {1}",
                    exception.GetType().Name,
                    exception.Message));

                refusals.AddRange(systemVentilationConversionContext.Refusals);

                return new SystemVentilationRoute(noIzamThermalSource, path_TPD, null, null, null, null, refusals, notes);
            }

            notes.AddRange(systemVentilationConversionContext.Notes);

            if (!converted || !systemVentilationConversionContext.IsReconciled)
            {
                refusals.AddRange(systemVentilationConversionContext.Refusals);

                if (refusals.Count == 0)
                {
                    refusals.Add("The conversion to TAS Systems did not complete, and said nothing about why.");
                }

                return new SystemVentilationRoute(noIzamThermalSource, path_TPD, null, null, null, null, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //4. The simulation - the AIR SYSTEMS, not the document.
            //-------------------------------------------------------------------------------------------
            List<SystemVentilationBinding> systemVentilationBindings = systemVentilationConversionContext.Bindings;

            profiler.Step("Route: TAS air-system simulation + ZoneTemperature");

            Modify.SimulateSystems(
                path_TPD,
                systemVentilationBindings,
                startHour,
                endHour,
                out SimulationEvidence simulationEvidence,
                out SystemZoneTemperatureResults systemZoneTemperatureResults);

            notes.AddRange(simulationEvidence.Notes);

            if (simulationEvidence.Refusals.Count != 0)
            {
                refusals.AddRange(simulationEvidence.Refusals);

                return new SystemVentilationRoute(noIzamThermalSource, path_TPD, simulationEvidence, null, null, null, refusals, notes);
            }

            //-------------------------------------------------------------------------------------------
            //5. The results, and the gate.
            //-------------------------------------------------------------------------------------------
            profiler.Step("Route: validating zone temperatures");

            systemZoneTemperatureResults.Validate(systemVentilationBindings);

            notes.AddRange(systemZoneTemperatureResults.Notes);

            if (!systemZoneTemperatureResults.IsComplete)
            {
                refusals.AddRange(systemZoneTemperatureResults.Refusals);

                simulationEvidence.RecordResultsNotReconciled(string.Format(
                    "{0} of {1} room(s) did not return a complete finite zone temperature series for hours {2}..{3}.",
                    systemZoneTemperatureResults.Refusals.Count,
                    systemVentilationBindings.Count,
                    startHour,
                    endHour));

                return new SystemVentilationRoute(noIzamThermalSource, path_TPD, simulationEvidence, null, null, null, refusals, notes);
            }

            simulationEvidence.RecordResultsReconciled(systemVentilationBindings.Count, startHour, endHour);

            //-------------------------------------------------------------------------------------------
            //6. PR5B (SAM#111): a document carrying recirculation cooling branches is complete only once
            //   every branch is evidenced to have behaved as built - no heating, no cooling below its gate,
            //   recirculation within its law's range, the published table at the coil, the ventilation at
            //   design. A document without branches (every B0) does not run this at all.
            //-------------------------------------------------------------------------------------------
            RecirculationCoolingResults recirculationCoolingResults = null;

            if (systemVentilationConversionContext.RecirculationCoolings.Count != 0)
            {
                profiler.Step("Route: recirculation cooling evidence");

                recirculationCoolingResults = Modify.RecirculationCoolingResults(
                    path_TPD,
                    noIzamThermalSource.Path_TSD,
                    systemVentilationConversionContext,
                    systemZoneTemperatureResults,
                    startHour,
                    endHour);

                notes.AddRange(recirculationCoolingResults.Notes);

                if (!recirculationCoolingResults.IsComplete)
                {
                    refusals.AddRange(recirculationCoolingResults.Refusals);

                    if (refusals.Count == 0)
                    {
                        refusals.Add("The recirculation cooling evidence did not complete, and said nothing about why.");
                    }

                    return new SystemVentilationRoute(noIzamThermalSource, path_TPD, simulationEvidence, null, null, null, refusals, notes);
                }
            }

            //-------------------------------------------------------------------------------------------
            //7. SAM#123: what each manufacturer-guidance cooling unit did, hour by hour. A read that
            //   cannot be made refuses; what the unit did is reported, not judged here.
            //-------------------------------------------------------------------------------------------
            GuidanceCoolingResults guidanceCoolingResults = null;

            if (systemVentilationConversionContext.GuidanceCoolings.Count != 0)
            {
                profiler.Step("Route: manufacturer-guidance evidence");

                guidanceCoolingResults = Modify.GuidanceCoolingResults(path_TPD, systemVentilationConversionContext, startHour, endHour);

                notes.AddRange(guidanceCoolingResults.Notes);

                if (!guidanceCoolingResults.IsComplete)
                {
                    refusals.AddRange(guidanceCoolingResults.Refusals);

                    if (refusals.Count == 0)
                    {
                        refusals.Add("The manufacturer-guidance evidence did not complete, and said nothing about why.");
                    }

                    return new SystemVentilationRoute(noIzamThermalSource, path_TPD, simulationEvidence, null, null, null, refusals, notes);
                }
            }

            return new SystemVentilationRoute(
                noIzamThermalSource,
                path_TPD,
                simulationEvidence,
                systemVentilationBindings,
                systemVentilationConversionContext.ConnectionBindings,
                systemZoneTemperatureResults,
                recirculationCoolingResults,
                guidanceCoolingResults,
                refusals,
                notes);
        }

        /// <summary>
        /// Everything that must be true before a single TAS call is made. Each failure is stated in full
        /// rather than returned as a bare false, and the path guard is here rather than deeper down
        /// because by the time the conversion runs the output file has already been deleted.
        /// </summary>
        private static bool TryGuard(
            NoIzamThermalSource noIzamThermalSource,
            MechanicalVentilationMaterialisation mechanicalVentilationMaterialisation,
            string path_TPD,
            int startHour,
            int endHour,
            List<string> refusals)
        {
            if (noIzamThermalSource == null)
            {
                refusals.Add("No thermal source was supplied, so there is nothing for the ventilation to act on.");
            }
            else if (!noIzamThermalSource.IsComplete)
            {
                refusals.Add("The thermal source is not complete, so the ventilation route would stand on an unfinished building.");
                refusals.AddRange(noIzamThermalSource.Refusals);
            }

            if (mechanicalVentilationMaterialisation == null)
            {
                refusals.Add("No materialised ventilation graph was supplied.");
            }
            else if (!mechanicalVentilationMaterialisation.IsMaterialised)
            {
                refusals.Add("The ventilation graph was refused by its own materialisation, so there is no design to convert.");
                refusals.AddRange(mechanicalVentilationMaterialisation.Refusals);
            }

            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                refusals.Add("No TAS Systems document path was supplied.");
            }

            if (endHour < startHour)
            {
                refusals.Add(string.Format("The requested period ends before it starts: {0}..{1}.", startHour, endHour));
            }

            if (refusals.Count != 0)
            {
                return false;
            }

            //The path guard. Convert.ToTPD deletes its output before it starts, so a path that collided
            //with the thermal source would destroy the building silently and only fail much later, for a
            //reason that would look like something else entirely.
            if (SamePath(path_TPD, noIzamThermalSource.Path_TBD) || SamePath(path_TPD, noIzamThermalSource.Path_TSD))
            {
                refusals.Add(string.Format(
                    "The TAS Systems document would be written over the thermal source itself ({0}). The conversion "
                    + "deletes its output first, so this would destroy the building the route stands on.",
                    path_TPD));

                return false;
            }

            string directory = Path.GetDirectoryName(path_TPD);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                refusals.Add(string.Concat("The directory the TAS Systems document would be written to does not exist: ", directory));

                return false;
            }

            return true;
        }

        private static bool SamePath(string path_1, string path_2)
        {
            if (string.IsNullOrWhiteSpace(path_1) || string.IsNullOrWhiteSpace(path_2))
            {
                return false;
            }

            try
            {
                return string.Equals(Path.GetFullPath(path_1), Path.GetFullPath(path_2), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                //An unresolvable path is compared as written rather than treated as different: the guard
                //must not be defeated by a path the framework cannot canonicalise.
                return string.Equals(path_1, path_2, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
