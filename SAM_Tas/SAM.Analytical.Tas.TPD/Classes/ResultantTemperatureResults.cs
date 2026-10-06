// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core.Tas;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// Every bridged room's <c>ResultantTemperature</c> for one period - what an
    /// <see cref="IResultantTemperatureProvider"/> answers, and all a consumer needs.
    /// <para>
    /// <b>The completeness gate is the constructor.</b> Every room the caller expected must have exactly one
    /// complete, finite series over the period; no series may belong to a room nobody expected; and any
    /// refusal raised on the way here stands. If anything fails, <see cref="IsComplete"/> is false and the
    /// payload is <b>empty</b> - no series and no result path - so a caller that ignores
    /// <see cref="Refusals"/> still cannot read half an answer. The same structural rule
    /// <see cref="SystemVentilationRoute"/> follows.
    /// </para>
    /// <para>
    /// <b>Provider-neutral.</b> <see cref="Method"/> names how the series were obtained for the record, and
    /// <see cref="SimulationEvidence"/> is the evidence of the run that produced them. Neither is needed to use
    /// the series, which is what lets the thermostat bridge be replaced by a native TAS Systems result without
    /// any consumer changing.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class ResultantTemperatureResults
    {
        private readonly Dictionary<Guid, ResultantTemperatureResult> results = new Dictionary<Guid, ResultantTemperatureResult>();
        private readonly List<string> refusals = new List<string>();
        private readonly List<string> notes = new List<string>();

        private readonly string path_Result;

        /// <param name="method">How the series were obtained, for the record.</param>
        /// <param name="startHour">0-based first hour of the period.</param>
        /// <param name="endHour">0-based last hour, inclusive.</param>
        /// <param name="guids_Space">Every room a series is required for.</param>
        /// <param name="resultantTemperatureResults">The series that came back.</param>
        /// <param name="path_Result">The file the series were read from. Withheld on a refusal.</param>
        /// <param name="simulationEvidence">The run that produced them. Kept on a refusal, as the diagnosis.</param>
        /// <param name="refusals">Refusals already raised upstream. Any one of them refuses the whole set.</param>
        /// <param name="notes">What was done.</param>
        public ResultantTemperatureResults(
            string method,
            int startHour,
            int endHour,
            IEnumerable<Guid> guids_Space,
            IEnumerable<ResultantTemperatureResult> resultantTemperatureResults,
            string path_Result,
            SimulationEvidence simulationEvidence,
            IEnumerable<string> refusals,
            IEnumerable<string> notes)
        {
            Method = method;
            StartHour = startHour;
            EndHour = endHour;
            SimulationEvidence = simulationEvidence;

            if (refusals != null)
            {
                foreach (string refusal in refusals)
                {
                    if (!string.IsNullOrWhiteSpace(refusal))
                    {
                        this.refusals.Add(refusal);
                    }
                }
            }

            if (notes != null)
            {
                this.notes.AddRange(notes);
            }

            Dictionary<Guid, ResultantTemperatureResult> dictionary = Validate(startHour, endHour, guids_Space, resultantTemperatureResults, simulationEvidence, this.refusals);

            if (this.refusals.Count != 0)
            {
                IsComplete = false;
                return;
            }

            foreach (KeyValuePair<Guid, ResultantTemperatureResult> keyValuePair in dictionary)
            {
                results[keyValuePair.Key] = keyValuePair.Value;
            }

            this.path_Result = path_Result;

            this.notes.Add(string.Format(
                "Resultant temperature complete for {0} of {0} room(s), {1} finite value(s) each over hours {2}..{3}.",
                results.Count,
                ExpectedCount,
                startHour,
                endHour));

            IsComplete = true;
        }

        /// <summary>How the series were obtained. For the record only; never needed to use them.</summary>
        public string Method { get; }

        /// <summary>0-based first hour of the period.</summary>
        public int StartHour { get; }

        /// <summary>0-based last hour of the period, inclusive.</summary>
        public int EndHour { get; }

        /// <summary>How many values each room has.</summary>
        public int ExpectedCount
        {
            get { return EndHour - StartHour + 1; }
        }

        /// <summary>The file the series were read from. Null on a refusal.</summary>
        public string Path_Result
        {
            get { return path_Result; }
        }

        /// <summary>The run that produced the series. Kept on a refusal, because it is the diagnosis.</summary>
        public SimulationEvidence SimulationEvidence { get; }

        /// <summary>Whether every expected room has one complete, finite series and nothing was refused.</summary>
        public bool IsComplete { get; }

        /// <summary>Every series, ordered by <c>Space.Guid</c>. Empty on a refusal.</summary>
        public List<ResultantTemperatureResult> Results
        {
            get
            {
                List<ResultantTemperatureResult> result = new List<ResultantTemperatureResult>(results.Values);
                result.Sort((x, y) => x.Guid_Space.CompareTo(y.Guid_Space));
                return result;
            }
        }

        /// <summary>One room's series, by analytical room guid. Null when there is none, and always on a refusal.</summary>
        public ResultantTemperatureResult Result(Guid guid_Space)
        {
            return results.TryGetValue(guid_Space, out ResultantTemperatureResult result) ? result : null;
        }

        /// <summary>Every reason the set is not usable. Never empty on a refusal.</summary>
        public List<string> Refusals
        {
            get { return new List<string>(refusals); }
        }

        /// <summary>What was done, including the lineage of the series.</summary>
        public List<string> Notes
        {
            get { return new List<string>(notes); }
        }

        private static Dictionary<Guid, ResultantTemperatureResult> Validate(
            int startHour,
            int endHour,
            IEnumerable<Guid> guids_Space,
            IEnumerable<ResultantTemperatureResult> resultantTemperatureResults,
            SimulationEvidence simulationEvidence,
            List<string> refusals)
        {
            Dictionary<Guid, ResultantTemperatureResult> result = new Dictionary<Guid, ResultantTemperatureResult>();

            if (endHour < startHour)
            {
                refusals.Add(string.Format("The period {0}..{1} contains no hours.", startHour, endHour));
                return result;
            }

            if (simulationEvidence != null && !simulationEvidence.Completed)
            {
                refusals.Add("The run that produced the resultant temperatures is not evidenced as complete.");
                refusals.AddRange(simulationEvidence.Refusals);
            }

            HashSet<Guid> expected = new HashSet<Guid>();

            if (guids_Space != null)
            {
                foreach (Guid guid in guids_Space)
                {
                    if (guid == Guid.Empty || !expected.Add(guid))
                    {
                        refusals.Add(string.Format("Room {0} is expected twice, or is not a room.", guid));
                    }
                }
            }

            if (expected.Count == 0)
            {
                refusals.Add("No room was expected, so no resultant temperature can be complete.");
                return result;
            }

            if (resultantTemperatureResults != null)
            {
                foreach (ResultantTemperatureResult resultantTemperatureResult in resultantTemperatureResults)
                {
                    if (resultantTemperatureResult == null)
                    {
                        continue;
                    }

                    Guid guid_Space = resultantTemperatureResult.Guid_Space;

                    if (result.ContainsKey(guid_Space))
                    {
                        refusals.Add(string.Format("Room {0} produced two resultant temperature series.", guid_Space));
                        continue;
                    }

                    result[guid_Space] = resultantTemperatureResult;

                    if (!expected.Contains(guid_Space))
                    {
                        refusals.Add(string.Format("A resultant temperature series came back for room {0}, which nobody asked for.", guid_Space));
                        continue;
                    }

                    if (resultantTemperatureResult.StartHour != startHour || resultantTemperatureResult.EndHour != endHour)
                    {
                        refusals.Add(string.Format(
                            "Room {0}: its resultant temperature covers hours {1}..{2} and the period is {3}..{4}.",
                            guid_Space,
                            resultantTemperatureResult.StartHour,
                            resultantTemperatureResult.EndHour,
                            startHour,
                            endHour));

                        continue;
                    }

                    string refusal = resultantTemperatureResult.Refusal();
                    if (refusal != null)
                    {
                        refusals.Add(refusal);
                    }
                }
            }

            List<Guid> guids = new List<Guid>(expected);
            guids.Sort();

            foreach (Guid guid in guids)
            {
                if (!result.ContainsKey(guid))
                {
                    refusals.Add(string.Format("Room {0}: no resultant temperature series came back for it.", guid));
                }
            }

            return result;
        }

        public override string ToString()
        {
            return IsComplete
                ? string.Format("Resultant temperature: {0} room(s), hours {1}..{2}, {3}", results.Count, StartHour, EndHour, Method)
                : string.Format("Resultant temperature REFUSED ({0} reason(s)).", refusals.Count);
        }
    }
}
