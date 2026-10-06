// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        /// <summary>
        /// Runs the TBD workflow to produce the thermal source a TAS Systems ventilation route needs: a
        /// simulated building carrying <b>no</b> mechanical ventilation of its own, plus the identity
        /// map from each analytical room to its TAS zone.
        /// <para>
        /// <b>Both cleanups are forced on here rather than left to the caller.</b> They default to false
        /// on <see cref="WorkflowSettings"/> so that no existing caller moves, and a Part O Iteration 3
        /// source that had only one of them applied would model the ventilation twice - once as the
        /// building's own IZAMs or <c>ticV</c> gain, once explicitly in TAS Systems - and no result check
        /// downstream could see it. A caller that supplied settings with them off is not silently
        /// corrected: the settings are copied and the copy is corrected, and the source says what was
        /// applied.
        /// </para>
        /// <para>
        /// <b>The zone map is read off the model the workflow hands back</b>, not off the one that went
        /// in: <c>Modify.UpdateIds</c> stamps <c>SpaceParameter.ZoneGuid</c> during the run, and the
        /// input model does not carry it. Measured on licensed TAS, that guid is the same identifier the
        /// TSD's zone loads answer, which is what makes the Systems conversion name-free.
        /// </para>
        /// <para>
        /// <b>The simulation is evidenced, not assumed.</b> A leftover TSD from an earlier run is
        /// deleted before the workflow starts, so the file that exists afterwards can only be this run's.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The design model. Never mutated - the workflow takes its own copy.</param>
        /// <param name="workflowSettings">
        /// The workflow settings. Copied; the copy has both cleanups forced on. Must state
        /// <c>Path_TBD</c>, and must have <c>Simulate</c> on - a source with no results is not a thermal
        /// source.
        /// </param>
        /// <param name="analyticalModel_Result">The stamped model the workflow produced, or null.</param>
        public static NoIzamThermalSource NoIzamThermalSource(AnalyticalModel analyticalModel, WorkflowSettings workflowSettings, out AnalyticalModel analyticalModel_Result)
        {
            analyticalModel_Result = null;

            List<string> refusals = new List<string>();
            List<string> notes = new List<string>();

            if (analyticalModel == null)
            {
                refusals.Add("No analytical model was supplied, so there is no building to simulate.");
            }

            if (workflowSettings == null)
            {
                refusals.Add("No workflow settings were supplied.");
            }
            else if (string.IsNullOrWhiteSpace(workflowSettings.Path_TBD))
            {
                refusals.Add("The workflow settings state no TBD path, so the thermal source has nowhere to be written.");
            }
            else if (!workflowSettings.Simulate)
            {
                refusals.Add("The workflow settings do not ask for a simulation, and a thermal source with no results is not one.");
            }

            if (refusals.Count != 0)
            {
                return new NoIzamThermalSource(
                    workflowSettings?.Path_TBD,
                    null,
                    false,
                    false,
                    null,
                    null,
                    refusals,
                    notes);
            }

            WorkflowSettings workflowSettings_NoIzam = new WorkflowSettings(workflowSettings)
            {
                AddIZAMs = false,
                RemoveIZAMs = true,
                RemoveMechanicalVentilationGains = true,
            };

            string path_TBD = workflowSettings_NoIzam.Path_TBD;
            string path_TSD = Query.Path_TSD(path_TBD);

            SimulationEvidence simulationEvidence = new SimulationEvidence(SimulationOutputShape.SeparateOutputFile, path_TBD, path_TSD);

            simulationEvidence.Prepare(DateTime.UtcNow);

            WorkflowCalculator workflowCalculator = new WorkflowCalculator(workflowSettings_NoIzam);

            try
            {
                analyticalModel_Result = workflowCalculator.Calculate(analyticalModel);
            }
            catch (Exception exception)
            {
                simulationEvidence.RecordCallFailed(string.Format("{0}: {1}", exception.GetType().Name, exception.Message));

                refusals.Add(string.Format(
                    "The thermal source workflow threw {0}: {1}",
                    exception.GetType().Name,
                    exception.Message));

                //The notes the workflow had already produced are the diagnosis of where it got to, so
                //they are kept on the failure path as well as the success one.
                if (workflowCalculator.Notes != null)
                {
                    notes.AddRange(workflowCalculator.Notes);
                }

                return new NoIzamThermalSource(
                    path_TBD,
                    path_TSD,
                    workflowSettings_NoIzam.RemoveIZAMs,
                    workflowSettings_NoIzam.RemoveMechanicalVentilationGains,
                    simulationEvidence,
                    null,
                    refusals,
                    notes);
            }

            if (workflowCalculator.Notes != null)
            {
                notes.AddRange(workflowCalculator.Notes);
            }

            if (analyticalModel_Result == null)
            {
                simulationEvidence.RecordCallFailed("the workflow produced no model.");
            }
            else
            {
                simulationEvidence.RecordCallReturned();
            }

            simulationEvidence.Conclude();

            Dictionary<Guid, string> zoneReferences = Query.ZoneReferences(analyticalModel_Result);

            if (zoneReferences == null || zoneReferences.Count == 0)
            {
                refusals.Add(
                    "The simulated model carries no SpaceParameter.ZoneGuid on any space, so no room can be tied to "
                    + "its TAS zone by identity.");
            }
            else
            {
                notes.Add(string.Format("{0} room(s) carry a TAS zone identity.", zoneReferences.Count));
            }

            return new NoIzamThermalSource(
                path_TBD,
                path_TSD,
                workflowSettings_NoIzam.RemoveIZAMs,
                workflowSettings_NoIzam.RemoveMechanicalVentilationGains,
                simulationEvidence,
                zoneReferences,
                refusals,
                notes);
        }
    }
}
