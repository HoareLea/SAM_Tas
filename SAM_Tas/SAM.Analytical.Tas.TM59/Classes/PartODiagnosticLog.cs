// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using SAM.Analytical;
using SAM.Core;

namespace SAM.Analytical.Tas.TM59
{
    /// <summary>
    /// Everything <see cref="PartODiagnosticLog"/> needs to build one run's records. Every list is read, never
    /// mutated - this class composes the outputs already on the Grasshopper canvas and decides nothing about
    /// Part O, Part F or TM59 itself.
    /// </summary>
    public class PartODiagnosticLogInput
    {
        /// <summary>
        /// The design-side model - the same wire that feeds <c>Tas.TSDQueryTM59Results._analyticalModel</c>.
        /// This is the ONE model input the log needs: <c>Modify.RunWorkflow</c> (via
        /// <c>WorkflowCalculator.Calculate</c>) carries <c>PartFSpaceData</c> and the Part-F-applied
        /// <c>InternalCondition</c> through unchanged, so there is no separate pre-workflow model to ask for.
        /// </summary>
        public AnalyticalModel AnalyticalModel_Design { get; set; }

        /// <summary>The simulated spaces, exactly as <c>Tas.TSDQueryTM59Results</c> reports them on its <c>spaces</c> output.</summary>
        public List<Space> Spaces_Simulated { get; set; }

        public List<OverheatingScenario> OverheatingScenarios { get; set; }

        /// <summary>From <c>SAMAnalytical.PreparePartOIteration.notes</c>.</summary>
        public List<string> Notes { get; set; }

        /// <summary>From <c>SAMAnalytical.PreparePartOIteration.refusals</c>.</summary>
        public List<string> Refusals { get; set; }

        public List<TMResult> MechanicalVentilationResults { get; set; }
        public List<TMResult> NaturalVentilationResults { get; set; }
        public List<TMResult> CorridorResults { get; set; }

        /// <summary>From <c>Tas.TSDQueryTM59Results.successful</c>. Null where that wire is not connected.</summary>
        public bool? TM59Successful { get; set; }

        /// <summary>From <c>SAMAnalytical.WorkflowgbXML.successful</c>. Null where that wire is not connected.</summary>
        public bool? WorkflowSuccessful { get; set; }

        public string TM52BuildingCategory { get; set; }

        public string Path_gbXML { get; set; }
        public string Path_TBD { get; set; }
        public string Path_TSD { get; set; }
    }

    /// <summary>The ordered records <see cref="PartODiagnosticLog.Build"/> produced, plus the counts a caller reports without re-scanning them.</summary>
    public class PartODiagnosticLogBuildResult
    {
        public List<JsonObject> Records { get; } = new List<JsonObject>();

        /// <summary>Populated only when hourly logging was requested and at least one extended result was available to read a series from.</summary>
        public List<JsonObject> HourlyRecords { get; } = new List<JsonObject>();

        public int SpaceCount { get; set; }
        public int ScenarioCount { get; set; }
        public int NoteCount { get; set; }
        public int RefusalCount { get; set; }
        public int UnassociatedCount { get; set; }
    }

    /// <summary>
    /// Builds and writes the Part O -&gt; TAS -&gt; TM59 diagnostic JSON Lines log.
    /// <para>
    /// <b>Diagnostic-only, by construction.</b> This class reads the objects a Part O run already produced -
    /// scenarios, spaces, TM59 results, Part F data - and writes what it finds. It calls no Part F, Part O or
    /// TM59 calculation; it derives no ventilation strategy, no scenario and no pass/fail. Where an association
    /// cannot be made from the public APIs this composes, that is recorded as an <c>unassociated</c> row or a
    /// refusal, never guessed at or silently dropped.
    /// </para>
    /// <para>
    /// <b>Free of TAS COM types</b>, matching <c>ApproximateResultantTemperatureMap</c> in
    /// <c>SAM.Analytical.Tas.TPD</c>: this project references <c>SAM.Analytical.Tas</c> (which does reference
    /// the TAS interop) for the two identity helpers below, but this file itself never touches a <c>TBD</c>,
    /// <c>TCD</c> or <c>TSD</c> type, so the CLR never has to load the interop to run it - which is what makes
    /// it testable without an installed TAS.
    /// </para>
    /// <para>
    /// <b>Every static-class call below is fully qualified.</b> This namespace, <c>SAM.Analytical.Tas.TM59</c>,
    /// declares its own <c>Query</c>, <c>Modify</c> and <c>Convert</c> static classes; an unqualified
    /// <c>Query.SimulationSpaceKey(...)</c> would silently bind to the wrong one instead of failing to compile -
    /// the same nested-namespace trap the Part F work hit before. See <c>GbXMLRouteIdentityAcceptanceTests.cs</c>.
    /// </para>
    /// </summary>
    public static class PartODiagnosticLog
    {
        public const string Schema = "PartODiagnostic:v1";

        /// <summary>
        /// <c>Modify.UpdateIds</c> (<c>SAM.Analytical.Tas</c>) strips <c>SpaceParameter.ZoneGuid</c> from every
        /// space before the loop that would otherwise look it up, so every space's <c>ZoneGuid</c> is in
        /// practice (re)assigned by a TAS zone NAME match during that workflow step, not read from a
        /// pre-existing guid. A <c>zoneGuid</c> identity mode below is therefore real - the value is present and
        /// equal on both sides by the time this log reads it - but it does not, on its own, prove the chain from
        /// design to simulation was ever free of a name match. This is pre-existing compliance behaviour and is
        /// only recorded here, not changed - see the plan's "Finding to report, NOT to fix here".
        /// </summary>
        public const string ZoneGuidProvenance = "assignedDuringWorkflow";

        public const string ZoneGuidProvenanceNote =
            "ZoneGuid is (re)assigned by Modify.UpdateIds during the TAS workflow, primarily by TAS zone name. " +
            "A zoneGuid identityMode confirms the key matches on both sides at read time; it does not by itself " +
            "prove the assignment upstream was free of a name match. Reported, not fixed, by this logger.";

        private const string ReasonNoTM59Result = "No TM59 result referenced this space's simulated guid.";

        /// <summary>
        /// The run record's <c>partOIteration</c> when its dwellings are assessed at more than one iteration - a
        /// mixed building (PR1 mixed dwelling strategies, PR3B cooling), where no single iteration is true of the run.
        /// Each space row still carries its own governing scenario's iteration.
        /// </summary>
        public const string MixedPartOIteration = "Mixed";

        // -----------------------------------------------------------------------------------------------
        // Build
        // -----------------------------------------------------------------------------------------------

        public static PartODiagnosticLogBuildResult Build(PartODiagnosticLogInput input, Guid runId, DateTime runTimestampUtc, bool includeHourly)
        {
            PartODiagnosticLogBuildResult result = new PartODiagnosticLogBuildResult();

            if (input == null)
            {
                return result;
            }

            List<Space> spaces_Design = input.AnalyticalModel_Design?.GetSpaces() ?? new List<Space>();
            List<Space> spaces_Simulated = input.Spaces_Simulated ?? new List<Space>();
            List<OverheatingScenario> scenarios = (input.OverheatingScenarios ?? new List<OverheatingScenario>()).FindAll(x => x != null);
            List<string> notes = input.Notes ?? new List<string>();
            List<string> refusals_PartOIteration = input.Refusals ?? new List<string>();

            List<TMResult> results = new List<TMResult>();
            results.AddRange((input.MechanicalVentilationResults ?? new List<TMResult>()).Where(x => x != null));
            results.AddRange((input.NaturalVentilationResults ?? new List<TMResult>()).Where(x => x != null));
            results.AddRange((input.CorridorResults ?? new List<TMResult>()).Where(x => x != null));

            // The two identity helpers this composes rather than reinvents - see Query.SimulationSpaceKey and
            // Create.SimulationSpaceMap in SAM.Analytical.Tas.
            SAM.Analytical.SimulationSpaceMap simulationSpaceMap = SAM.Analytical.Tas.Create.SimulationSpaceMap(spaces_Design, spaces_Simulated);

            // Constructed whenever scenarios were supplied, even with a null design model - OverheatingScenarioMap
            // itself refuses cleanly and explains why (see PartOResultAssociationTests, "no design model"/"no
            // simulation space map"), and that refusal is worth surfacing rather than silently skipping the map.
            SAM.Analytical.OverheatingScenarioMap overheatingScenarioMap = null;
            if (scenarios.Count > 0)
            {
                overheatingScenarioMap = new SAM.Analytical.OverheatingScenarioMap(scenarios, input.AnalyticalModel_Design, simulationSpaceMap);
            }

            List<string> refusals_ScenarioMap = overheatingScenarioMap?.Refusals ?? new List<string>();

            bool extendedResultsSupplied = results.Any(x => x is TMExtendedResult);

            // -------------------------------------------------------------------------------------------
            // run
            // -------------------------------------------------------------------------------------------

            JsonObject run = NewRecord("run", runId, runTimestampUtc);
            SetString(run, "modelName", input.AnalyticalModel_Design?.Name);
            SetString(run, "modelGuid", input.AnalyticalModel_Design?.Guid.ToString());
            SetString(run, "partOIteration", RunPartOIteration(scenarios));
            run["partOIterations"] = PartOIterations(scenarios);
            SetString(run, "tM52BuildingCategory", input.TM52BuildingCategory);
            SetString(run, "path_gbXML", input.Path_gbXML);
            SetString(run, "path_TBD", input.Path_TBD);
            SetString(run, "path_TSD", input.Path_TSD);
            SetNullableBool(run, "workflowSuccessful", input.WorkflowSuccessful);
            SetNullableBool(run, "tM59Successful", input.TM59Successful);
            run["simulationSpaceMapIsComplete"] = simulationSpaceMap.IsComplete;
            SetNullableBool(run, "overheatingScenarioMapIsComplete", overheatingScenarioMap == null ? (bool?)null : overheatingScenarioMap.IsComplete);
            run["extendedResultsSupplied"] = extendedResultsSupplied;
            SetString(run, "zoneGuidProvenance", ZoneGuidProvenance);
            SetString(run, "zoneGuidProvenanceNote", ZoneGuidProvenanceNote);

            // Counts are filled in below, once the space/scenario/unassociated rows are known - the run record
            // is still appended first so it stays first in the file regardless.
            result.Records.Add(run);

            // -------------------------------------------------------------------------------------------
            // scenario
            // -------------------------------------------------------------------------------------------

            List<OverheatingScenario> scenarios_Ordered = scenarios.OrderBy(x => x.Key).ToList();
            foreach (OverheatingScenario scenario in scenarios_Ordered)
            {
                JsonObject record = NewRecord("scenario", runId, runTimestampUtc);
                SetString(record, "key", scenario.Key.ToString());
                record["scenario"] = scenario.ToJsonObject();

                List<Space> governed = overheatingScenarioMap?.Spaces(scenario) ?? new List<Space>();
                JsonArray governedGuids = new JsonArray();
                foreach (Space space in governed.Where(x => x != null).OrderBy(x => x.Guid))
                {
                    governedGuids.Add(space.Guid.ToString());
                }
                record["governedSimulatedSpaceGuids"] = governedGuids;

                result.Records.Add(record);
            }

            result.ScenarioCount = scenarios_Ordered.Count;

            // -------------------------------------------------------------------------------------------
            // space - one row per assessed space, and "assessed" means governed by a scenario. A result whose
            // Reference resolves to no governing scenario is deliberately NOT folded into a space row here - it
            // is reported only as an "unassociated" result record below. Mixing the two would let a stray
            // result quietly acquire a design-side identity (Part F data, ventilation strategy) it was never
            // actually governed under.
            // -------------------------------------------------------------------------------------------

            Dictionary<Guid, TMResult> result_BySimulatedGuid = new Dictionary<Guid, TMResult>();
            foreach (TMResult tMResult in results)
            {
                if (Guid.TryParse(tMResult.Reference, out Guid guid_Result))
                {
                    // Last one wins if two results somehow reference the same space - deterministic given the
                    // three input lists are appended in a fixed order (mechanical, natural, corridor) above.
                    result_BySimulatedGuid[guid_Result] = tMResult;
                }
            }

            Dictionary<Guid, Space> spacesToAssess = new Dictionary<Guid, Space>();

            if (overheatingScenarioMap != null)
            {
                foreach (OverheatingScenario scenario in scenarios)
                {
                    foreach (Space space in overheatingScenarioMap.Spaces(scenario) ?? new List<Space>())
                    {
                        if (space != null)
                        {
                            spacesToAssess[space.Guid] = space;
                        }
                    }
                }
            }

            List<(string scenarioKey, string zoneGuid, string designGuid, string simulatedGuid, JsonObject record)> space_Rows = new List<(string, string, string, string, JsonObject)>();
            List<(string zoneGuid, string simulatedGuid, JsonObject record)> hourly_Rows = new List<(string, string, JsonObject)>();

            foreach (Space space_Simulation in spacesToAssess.Values)
            {
                Space space_Design = simulationSpaceMap.Design(space_Simulation);
                OverheatingScenario scenario_Governing = overheatingScenarioMap?.Scenario(space_Simulation);
                result_BySimulatedGuid.TryGetValue(space_Simulation.Guid, out TMResult tMResult);

                string identityMode = IdentityMode(simulationSpaceMap, space_Simulation, space_Design);

                // Logged separately, never coalesced: a coalesced single value can read as a match even where
                // the two sides disagree or one is blank, which is exactly the ambiguity an identity
                // investigation (see ZoneGuidProvenanceNote) needs resolved rather than hidden.
                string designZoneGuidRaw = space_Design == null ? null : SAM.Analytical.Tas.Query.SimulationSpaceKey(space_Design);
                string simulatedZoneGuidRaw = SAM.Analytical.Tas.Query.SimulationSpaceKey(space_Simulation);
                string zoneGuid_ForOrdering = designZoneGuidRaw ?? simulatedZoneGuidRaw ?? string.Empty;

                // SAM.Analytical.Tas.TM59's enclosing namespace SAM.Analytical.Tas declares its own
                // SpaceParameter (ZoneGuid only) - an unqualified SpaceParameter here would silently bind to
                // that one instead of SAM.Analytical.SpaceParameter, the trap this class's own summary warns
                // about. PartFSpaceData lives on the latter, so it is qualified explicitly.
                PartFSpaceData partFSpaceData = space_Design?.GetValue<PartFSpaceData>(SAM.Analytical.SpaceParameter.PartFSpaceData);

                JsonObject record = NewRecord("space", runId, runTimestampUtc);

                SetString(record, "designSpaceGuid", space_Design?.Guid.ToString());
                SetString(record, "designSpaceName", space_Design?.Name);
                SetString(record, "simulatedSpaceGuid", space_Simulation.Guid.ToString());
                SetString(record, "simulatedSpaceName", space_Simulation.Name);
                SetString(record, "designZoneGuidRaw", designZoneGuidRaw);
                SetString(record, "simulatedZoneGuidRaw", simulatedZoneGuidRaw);
                SetString(record, "identityMode", identityMode);

                SetNullableDouble(record, "continuousDesignFlowRate_Lps", partFSpaceData?.ContinuousDesignFlowRate_Lps);
                SetNullableDouble(record, "setbackFlowRate_Lps", partFSpaceData?.SetbackFlowRate_Lps);

                // continuousDesignFlowRate_Lps above is the LEGACY single-terminal figure - PartFSpaceData's own
                // PrimaryTerminal only (the supply terminal of a habitable room, the extract terminal of a wet
                // room) - kept for direct comparison with SAMAnalytical.AddVentilationPropertiesByPartF's own
                // "l/s" output. A multi-terminal space (a studio or open-plan living kitchen, Approved Document F
                // Appendix A) also has a SECONDARY terminal invisible through that scalar - the local kitchen
                // extract SAM_UI's Part F Conformance Assessment window shows as its own "KEX" tag alongside
                // "SUP". The three fields below read PartFSpaceData's own already-computed per-role totals -
                // nothing here is a new calculation - so the two workflows show the same breakdown instead of
                // this log silently dropping the secondary terminal the way the legacy scalar does.
                SetNullableDouble(record, "supplyFlowRate_Lps", partFSpaceData?.ContinuousSupplyFlowRate_Lps);
                SetNullableDouble(record, "extractFlowRate_Lps", partFSpaceData?.ContinuousExtractFlowRate_Lps);
                SetNullableDouble(record, "localKitchenExtractFlowRate_Lps", partFSpaceData?.LocalKitchenExtractFlowRate_Lps);

                SetNullableDouble(record, "suppliedAirFlow_m3s", SuppliedAirFlow_m3s(space_Design));
                SetNullableDouble(record, "exhaustAirFlow_m3s", ExhaustAirFlow_m3s(space_Design));

                SetString(record, "ventilationStrategy", scenario_Governing?.VentilationStrategy);
                SetString(record, "partOIteration", scenario_Governing == null ? null : scenario_Governing.Iteration.ToString());
                SetString(record, "scenarioKey", scenario_Governing == null ? (string)null : scenario_Governing.Key.ToString());

                SetString(record, "criterion", Criterion(tMResult));
                SetNullableBool(record, "pass", tMResult?.Pass);
                SetNullableInt(record, "occupiedHours", tMResult == null ? (int?)null : tMResult.OccupiedHours);
                SetNullableInt(record, "maxExceedableHours", tMResult == null ? (int?)null : tMResult.MaxExceedableHours);
                SetCriterionSpecificFields(record, tMResult);

                SetString(record, "tM59ResultAbsentReason", tMResult == null ? ReasonNoTM59Result : null);

                record["partFSpaceData"] = partFSpaceData?.ToJsonObject();
                record["tM59Result"] = tMResult?.ToJsonObject();
                record["scenario"] = scenario_Governing?.ToJsonObject();

                space_Rows.Add((scenario_Governing == null ? null : scenario_Governing.Key.ToString(), zoneGuid_ForOrdering, space_Design?.Guid.ToString(), space_Simulation.Guid.ToString(), record));

                if (includeHourly && tMResult != null)
                {
                    string simulatedGuid_Hourly = space_Simulation.Guid.ToString();
                    foreach (JsonObject hourlyRecord in BuildHourlyRecords(tMResult, space_Design, space_Simulation, designZoneGuidRaw, simulatedZoneGuidRaw, identityMode, runId, runTimestampUtc))
                    {
                        hourly_Rows.Add((zoneGuid_ForOrdering, simulatedGuid_Hourly, hourlyRecord));
                    }
                }
            }

            // Ordered by (scenario key, zoneGuid, design space guid) per the spec, with the simulated space guid
            // as a final tiebreaker so the order is fully deterministic even if two rows tie on all three -
            // Dictionary<> enumeration order is not a documented guarantee to rely on for that.
            foreach (var row in space_Rows.OrderBy(x => x.scenarioKey ?? string.Empty, StringComparer.Ordinal)
                                           .ThenBy(x => x.zoneGuid ?? string.Empty, StringComparer.Ordinal)
                                           .ThenBy(x => x.designGuid ?? string.Empty, StringComparer.Ordinal)
                                           .ThenBy(x => x.simulatedGuid, StringComparer.Ordinal))
            {
                result.Records.Add(row.record);
            }

            result.SpaceCount = space_Rows.Count;

            // Same determinism reasoning as the space rows above: grouped by space, with the fixed
            // operativeTemperature/minAcceptable/maxAcceptable order within a space preserved by OrderBy's
            // stable-sort guarantee.
            foreach (var row in hourly_Rows.OrderBy(x => x.zoneGuid ?? string.Empty, StringComparer.Ordinal)
                                            .ThenBy(x => x.simulatedGuid, StringComparer.Ordinal))
            {
                result.HourlyRecords.Add(row.record);
            }

            // -------------------------------------------------------------------------------------------
            // note / refusal
            // -------------------------------------------------------------------------------------------

            foreach (string note in notes)
            {
                JsonObject record = NewRecord("note", runId, runTimestampUtc);
                SetString(record, "origin", "partOIteration");
                SetString(record, "text", note);
                result.Records.Add(record);
            }

            result.NoteCount = notes.Count;

            int refusalCount = 0;

            foreach (string refusal in refusals_PartOIteration)
            {
                JsonObject record = NewRecord("refusal", runId, runTimestampUtc);
                SetString(record, "origin", "partOIteration");
                SetString(record, "text", refusal);
                result.Records.Add(record);
                refusalCount++;
            }

            foreach (string refusal in refusals_ScenarioMap)
            {
                JsonObject record = NewRecord("refusal", runId, runTimestampUtc);
                SetString(record, "origin", "scenarioMap");
                SetString(record, "text", refusal);
                result.Records.Add(record);
                refusalCount++;
            }

            result.RefusalCount = refusalCount;

            // -------------------------------------------------------------------------------------------
            // unassociated - results that tied to no scenario, and spaces the SimulationSpaceMap left
            // Unresolved/Ambiguous. This record existing at all is the signal to investigate.
            // -------------------------------------------------------------------------------------------

            int unassociatedCount = 0;

            List<TMResult> results_Unassociated = results
                .Where(x => overheatingScenarioMap?.Scenario(x) == null)
                .OrderBy(x => x.Reference ?? string.Empty, StringComparer.Ordinal)
                .ToList();

            foreach (TMResult tMResult in results_Unassociated)
            {
                JsonObject record = NewRecord("unassociated", runId, runTimestampUtc);
                SetString(record, "kind", "result");
                SetString(record, "reference", tMResult.Reference);
                SetString(record, "name", tMResult.Name);
                SetString(record, "criterion", Criterion(tMResult));
                result.Records.Add(record);
                unassociatedCount++;
            }

            foreach (Space space in (simulationSpaceMap.Unresolved ?? new List<Space>()).Where(x => x != null).OrderBy(x => x.Guid))
            {
                result.Records.Add(UnassociatedSpaceRecord(space, "simulated", "unresolved", runId, runTimestampUtc));
                unassociatedCount++;
            }

            foreach (Space space in (simulationSpaceMap.Ambiguous ?? new List<Space>()).Where(x => x != null).OrderBy(x => x.Guid))
            {
                result.Records.Add(UnassociatedSpaceRecord(space, "simulated", "ambiguous", runId, runTimestampUtc));
                unassociatedCount++;
            }

            foreach (Space space in (simulationSpaceMap.AmbiguousDesign ?? new List<Space>()).Where(x => x != null).OrderBy(x => x.Guid))
            {
                result.Records.Add(UnassociatedSpaceRecord(space, "design", "ambiguous", runId, runTimestampUtc));
                unassociatedCount++;
            }

            result.UnassociatedCount = unassociatedCount;

            // Now that every downstream count is known, fill in the counts on the run record already in
            // Records[0].
            run["spaceCount"] = result.SpaceCount;
            run["scenarioCount"] = result.ScenarioCount;
            run["noteCount"] = result.NoteCount;
            run["refusalCount"] = result.RefusalCount;
            run["unassociatedCount"] = result.UnassociatedCount;
            run["mechanicalResultCount"] = (input.MechanicalVentilationResults ?? new List<TMResult>()).Count(x => x != null);
            run["naturalResultCount"] = (input.NaturalVentilationResults ?? new List<TMResult>()).Count(x => x != null);
            run["corridorResultCount"] = (input.CorridorResults ?? new List<TMResult>()).Count(x => x != null);

            return result;
        }

        // -----------------------------------------------------------------------------------------------
        // Hourly
        // -----------------------------------------------------------------------------------------------

        private static readonly (string key, Func<TMExtendedResult, IndexedDoubles> selector)[] HourlySeries =
        {
            ("operativeTemperature", x => x.OperativeTemperatures),
            ("minAcceptable", x => x.MinAcceptableTemperatures),
            ("maxAcceptable", x => x.MaxAcceptableTemperatures),
        };

        private static List<JsonObject> BuildHourlyRecords(TMResult tMResult, Space space_Design, Space space_Simulation, string designZoneGuidRaw, string simulatedZoneGuidRaw, string identityMode, Guid runId, DateTime runTimestampUtc)
        {
            List<JsonObject> result = new List<JsonObject>();

            TMExtendedResult tMExtendedResult = tMResult as TMExtendedResult;

            foreach ((string key, Func<TMExtendedResult, IndexedDoubles> selector) in HourlySeries)
            {
                JsonObject record = NewRecord(tMExtendedResult == null ? "refusal" : "hourly", runId, runTimestampUtc);

                //Identity fields are set on EVERY record, including the refusal below - this logger exists
                //to diagnose duplicate room names, and a refusal without its space identity is exactly the
                //row that cannot be attributed to a flat.
                SetString(record, "designSpaceGuid", space_Design?.Guid.ToString());
                SetString(record, "simulatedSpaceGuid", space_Simulation?.Guid.ToString());
                SetString(record, "designZoneGuidRaw", designZoneGuidRaw);
                SetString(record, "simulatedZoneGuidRaw", simulatedZoneGuidRaw);
                SetString(record, "identityMode", identityMode);
                SetString(record, "series", key);

                if (tMExtendedResult == null)
                {
                    SetString(record, "origin", "hourly");
                    SetString(record, "text", string.Format(
                        "Space '{0}' has no extended TM59 result (Tas.TSDQueryTM59Results was not run with _extended_ = true), so its '{1}' hourly series cannot be logged.",
                        space_Simulation?.Name, key));
                    result.Add(record);
                    continue;
                }

                IndexedDoubles indexedDoubles = selector(tMExtendedResult);

                int? minIndex = indexedDoubles?.GetMinIndex();
                int? maxIndex = indexedDoubles?.GetMaxIndex();

                if (indexedDoubles == null || !minIndex.HasValue || !maxIndex.HasValue)
                {
                    record["record"] = "refusal";
                    SetString(record, "origin", "hourly");
                    SetString(record, "text", string.Format("Space '{0}' produced no readable '{1}' series - nothing to log.", space_Simulation?.Name, key));
                    result.Add(record);
                    continue;
                }

                // The series is embedded via IndexedDoubles' own ToJsonObject(): explicit [index, value] pairs
                // per populated index (SAM.Core.Classes.Collection.IndexedObjects.ToJsonObject), never a bare
                // array - so a non-zero start index or a gap is represented exactly, not wrapped or stretched
                // into a plausible-looking full year. See ApproximateResultantTemperatureMap.cs for the
                // equivalent reasoning on the TPD-preparation route.
                record["minIndex"] = minIndex.Value;
                record["maxIndex"] = maxIndex.Value;
                record["count"] = indexedDoubles.Count;
                record["indexedDoubles"] = indexedDoubles.ToJsonObject();

                result.Add(record);
            }

            return result;
        }

        // -----------------------------------------------------------------------------------------------
        // Write
        // -----------------------------------------------------------------------------------------------

        public static bool TryWriteJsonLines(IEnumerable<JsonObject> records, string path, out string error)
        {
            error = null;

            if (records == null || string.IsNullOrWhiteSpace(path))
            {
                error = "No records or no path supplied.";
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                System.Text.Json.JsonSerializerOptions serializerOptions = SAM.Core.Convert.SerializerOptions(SAM.Core.Formatting.None);

                List<string> lines = new List<string>();
                foreach (JsonObject record in records)
                {
                    lines.Add(record.ToJsonString(serializerOptions));
                }

                File.WriteAllLines(path, lines);

                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// The one Part O iteration a run is assessed at, read from its scenarios - never from whichever scenario
        /// happens to be first. Dwellings state the run's iteration; a common space's <c>DwellingIndependent</c> is a
        /// neutral identity, not an iteration of the run, so it is read only when there is no dwelling scenario at
        /// all. One distinct iteration → its name; more than one → <see cref="MixedPartOIteration"/>; no scenario →
        /// null.
        /// </summary>
        public static string RunPartOIteration(IEnumerable<OverheatingScenario> overheatingScenarios)
        {
            List<OverheatingScenario> scenarios = overheatingScenarios?.Where(x => x != null).ToList() ?? new List<OverheatingScenario>();

            List<OverheatingScenario> scenarios_Dwelling = scenarios.FindAll(x => x.Scope != SAM.Analytical.Enums.PartOAssessmentScope.CommonSpace);
            if (scenarios_Dwelling.Count != 0)
            {
                scenarios = scenarios_Dwelling;
            }

            List<SAM.Analytical.Enums.PartOIteration> iterations = scenarios.Select(x => x.Iteration).Distinct().ToList();

            switch (iterations.Count)
            {
                case 0:
                    return null;

                case 1:
                    return iterations[0].ToString();

                default:
                    return MixedPartOIteration;
            }
        }

        // -----------------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------------

        /// <summary>Every distinct iteration the scenarios state, common spaces included, ordinal-sorted by name.</summary>
        private static JsonArray PartOIterations(List<OverheatingScenario> scenarios)
        {
            JsonArray result = new JsonArray();
            foreach (string iteration in scenarios.Select(x => x.Iteration.ToString()).Distinct().OrderBy(x => x, StringComparer.Ordinal))
            {
                result.Add(iteration);
            }

            return result;
        }

        private static string IdentityMode(SAM.Analytical.SimulationSpaceMap simulationSpaceMap, Space space_Simulation, Space space_Design)
        {
            if (simulationSpaceMap == null || space_Simulation == null)
            {
                return "unresolved";
            }

            if ((simulationSpaceMap.Ambiguous ?? new List<Space>()).Any(x => x != null && x.Guid == space_Simulation.Guid))
            {
                return "ambiguous";
            }

            if (space_Design != null && (simulationSpaceMap.AmbiguousDesign ?? new List<Space>()).Any(x => x != null && x.Guid == space_Design.Guid))
            {
                return "ambiguous";
            }

            if ((simulationSpaceMap.Unresolved ?? new List<Space>()).Any(x => x != null && x.Guid == space_Simulation.Guid))
            {
                return "unresolved";
            }

            if (space_Design == null)
            {
                return "unresolved";
            }

            string key_Simulation = SAM.Analytical.Tas.Query.SimulationSpaceKey(space_Simulation);
            string key_Design = SAM.Analytical.Tas.Query.SimulationSpaceKey(space_Design);

            return !string.IsNullOrWhiteSpace(key_Simulation) && !string.IsNullOrWhiteSpace(key_Design) && key_Simulation == key_Design
                ? "zoneGuid"
                : "uniqueName";
        }

        /// <summary>
        /// The TM59-extended result types (<c>Tas.TSDQueryTM59Results._extended_ = true</c>) are NOT subtypes
        /// of their plain siblings - <c>TM59MechanicalVentilationExtendedResult</c> extends
        /// <c>TM59ExtendedResult</c>/<c>TMExtendedResult</c>, a separate branch from
        /// <c>TM59MechanicalVentilationResult</c>. Both branches have to be checked explicitly; a bedroom
        /// variant is checked before its non-bedroom parent on each branch, since
        /// <c>TM59NaturalVentilationBedroomExtendedResult</c> also matches <c>is TM59NaturalVentilationExtendedResult</c>.
        /// </summary>
        private static string Criterion(TMResult tMResult)
        {
            if (tMResult is TM59NaturalVentilationBedroomResult || tMResult is TM59NaturalVentilationBedroomExtendedResult) return "naturalBedroom";
            if (tMResult is TM59NaturalVentilationResult || tMResult is TM59NaturalVentilationExtendedResult) return "natural";
            if (tMResult is TM59CorridorResult || tMResult is TM59CorridorExtendedResult) return "corridor";
            if (tMResult is TM59MechanicalVentilationResult || tMResult is TM59MechanicalVentilationExtendedResult) return "mechanical";
            return null;
        }

        /// <summary>-1 is the extended-result branch's sentinel for "no readable series to count from" (see e.g. <c>TM59MechanicalVentilationExtendedResult.GetHoursNumberExceeding26</c>) - reported as null, the same as an absent value, not as a fabricated negative count.</summary>
        private static int? NonNegative(int value)
        {
            return value < 0 ? (int?)null : value;
        }

        private static void SetCriterionSpecificFields(JsonObject record, TMResult tMResult)
        {
            // Plain (non-extended) branch: the counts are already-computed properties.
            SetNullableInt(record, "hoursExceeding26", (tMResult as TM59MechanicalVentilationResult)?.HoursExceeding26);
            SetNullableInt(record, "hoursExceeding28", (tMResult as TM59CorridorResult)?.HoursExceeding28);
            SetNullableInt(record, "hoursExceedingComfortRange", (tMResult as TM59NaturalVentilationResult)?.HoursExceedingComfortRange);

            // TAS's own TM59 report states a natural-ventilation day criterion against "Occupied Summer Hours" /
            // "Max. Exceedable Hours" - a different, smaller basis than the whole-year occupiedHours/
            // maxExceedableHours logged above (which TM59NaturalVentilationResult also carries, for the
            // criterion's underlying annual bookkeeping). Without these two, a reader comparing this log against
            // a TAS report would see occupiedHours/maxExceedableHours and wrongly read them as the day
            // criterion's threshold.
            SetNullableInt(record, "summerOccupiedHours", (tMResult as TM59NaturalVentilationResult)?.SummerOccupiedHours);
            SetNullableInt(record, "maxExceedableSummerHours", (tMResult as TM59NaturalVentilationResult)?.MaxExceedableSummerHours);

            TM59NaturalVentilationBedroomResult bedroom = tMResult as TM59NaturalVentilationBedroomResult;
            SetNullableInt(record, "annualNightOccupiedHours", bedroom?.AnnualNightOccupiedHours);
            SetNullableInt(record, "maxExceedableNightHours", bedroom?.MaxExceedableNightHours);
            SetNullableInt(record, "nightHoursNumberExceeding26", bedroom?.NightHoursNumberExceeding26);

            // Extended branch: the equivalent counts are derived on demand from the hourly series by Get*()
            // methods on the extended types themselves - composed here, not recomputed.
            if (tMResult is TM59MechanicalVentilationExtendedResult mechanicalExtended)
            {
                SetNullableInt(record, "hoursExceeding26", NonNegative(mechanicalExtended.GetHoursNumberExceeding26()));
            }
            else if (tMResult is TM59CorridorExtendedResult corridorExtended)
            {
                SetNullableInt(record, "hoursExceeding28", NonNegative(corridorExtended.GetHoursNumberExceeding28()));
            }
            else if (tMResult is TM59NaturalVentilationBedroomExtendedResult bedroomExtended)
            {
                SetNullableInt(record, "hoursExceedingComfortRange", NonNegative(bedroomExtended.GetSummerOccupiedHoursExceedingComfortRange()));
                SetNullableInt(record, "summerOccupiedHours", NonNegative(bedroomExtended.GetSummerOccupiedHours()));
                SetNullableInt(record, "maxExceedableSummerHours", NonNegative(bedroomExtended.GetSummerMaxExceedableHours()));
                SetNullableInt(record, "annualNightOccupiedHours", NonNegative(bedroomExtended.GetAnnualNightOccupiedHours()));
                SetNullableInt(record, "maxExceedableNightHours", NonNegative(bedroomExtended.GetAnnualMaxExceedableNightHours()));
                SetNullableInt(record, "nightHoursNumberExceeding26", NonNegative(bedroomExtended.GetNightHoursNumberExceeding26()));
            }
            else if (tMResult is TM59NaturalVentilationExtendedResult naturalExtended)
            {
                SetNullableInt(record, "hoursExceedingComfortRange", NonNegative(naturalExtended.GetSummerOccupiedHoursExceedingComfortRange()));
                SetNullableInt(record, "summerOccupiedHours", NonNegative(naturalExtended.GetSummerOccupiedHours()));
                SetNullableInt(record, "maxExceedableSummerHours", NonNegative(naturalExtended.GetSummerMaxExceedableHours()));
            }
        }

        /// <summary>The applied supply rate, read the same way the Part O component reads it back: through the query the simulation itself uses.</summary>
        private static double? SuppliedAirFlow_m3s(Space space_Design)
        {
            if (space_Design == null)
            {
                return null;
            }

            double value = SAM.Analytical.Query.CalculatedSupplyAirFlow(space_Design);
            return double.IsNaN(value) ? (double?)null : value;
        }

        private static double? ExhaustAirFlow_m3s(Space space_Design)
        {
            if (space_Design?.InternalCondition != null && space_Design.InternalCondition.TryGetValue(SAM.Analytical.InternalConditionParameter.ExhaustAirFlow, out double value))
            {
                return value;
            }

            return null;
        }

        private static JsonObject UnassociatedSpaceRecord(Space space, string side, string reason, Guid runId, DateTime runTimestampUtc)
        {
            JsonObject record = NewRecord("unassociated", runId, runTimestampUtc);
            SetString(record, "kind", "space");
            SetString(record, "side", side);
            SetString(record, "reason", reason);
            SetString(record, "spaceGuid", space?.Guid.ToString());
            SetString(record, "spaceName", space?.Name);
            return record;
        }

        private static JsonObject NewRecord(string record, Guid runId, DateTime runTimestampUtc)
        {
            return new JsonObject
            {
                ["schema"] = Schema,
                ["record"] = record,
                ["runId"] = runId.ToString(),
                ["runTimestampUtc"] = runTimestampUtc.ToString("o", CultureInfo.InvariantCulture),
            };
        }

        /// <summary>Always sets the key, to a JSON string or a JSON null - the key is never omitted, so an absent value is visibly absent rather than missing.</summary>
        private static void SetString(JsonObject jsonObject, string name, string value)
        {
            jsonObject[name] = value;
        }

        private static void SetNullableDouble(JsonObject jsonObject, string name, double? value)
        {
            jsonObject[name] = value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value)
                ? JsonValue.Create(value.Value)
                : null;
        }

        private static void SetNullableInt(JsonObject jsonObject, string name, int? value)
        {
            jsonObject[name] = value.HasValue && value.Value != int.MinValue
                ? JsonValue.Create(value.Value)
                : null;
        }

        private static void SetNullableBool(JsonObject jsonObject, string name, bool? value)
        {
            jsonObject[name] = value.HasValue ? JsonValue.Create(value.Value) : null;
        }
    }
}
