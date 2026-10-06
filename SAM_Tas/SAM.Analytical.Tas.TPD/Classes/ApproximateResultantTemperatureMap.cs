// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The <b>legacy, approximate</b> TPD preparation: one pass, no second simulation, and a
    /// <c>ResultantTemperature</c> series that is <b>synthesised</b> as the mean of the companion TSD's mean
    /// radiant temperature and the TPD's zone temperature.
    /// <para>
    /// <b>This is not the TPD-full route and must stay visibly distinct from it.</b> The authoritative TPD-full
    /// route is <see cref="ResultantTemperaturePreparation"/>: it simulates the actual system, carries the first
    /// pass's result into a copy of the TBD, simulates that copy, and reads a real <c>ResultantTemperature</c>
    /// out of the second TSD. This class does none of that. It exists because it shipped, and callers depend on
    /// it; it is kept as a compatibility route, not as an alternative answer to the same question. A failed
    /// TPD-full run must <b>never</b> fall back to it - reporting a synthesised series in place of a simulated
    /// one would present an approximation as the real thing.
    /// </para>
    /// <para>
    /// <b>Why the arithmetic lives here and not in <c>SAM.Analytical</c>.</b> Averaging mean radiant temperature
    /// with a systems-model zone temperature is a TAS-specific accommodation for a series TPD does not produce.
    /// <c>TM59AssessmentCalculator</c> assesses whatever hourly <c>ResultantTemperature</c> it is handed and must
    /// not learn where the series came from, or the engine-neutral half of the boundary is lost. Preparation
    /// differs; assessment does not.
    /// </para>
    /// <para>
    /// <b>Association is identity-first.</b> The route it replaced took the first TPD result whose <i>name</i>
    /// matched the space, which in a block of flats silently pairs one dwelling's system results with another
    /// dwelling's room - every flat has a "Bedroom 2". Where an identity cannot be resolved unambiguously the
    /// space is refused and reported, never paired with a same-named one.
    /// </para>
    /// <para>
    /// <b>UNVERIFIED, and it decides whether identity mode engages at all.</b> A space's identity is the TBD/TSD
    /// zone guid (<c>SpaceParameter.ZoneGuid</c>, which <c>ActiveSetting</c> maps to <c>TSD.ZoneData.zoneGUID</c>).
    /// A <c>SystemSpaceResult</c>'s <c>Reference</c> comes from <c>(systemZone as dynamic).GUID</c> in
    /// <c>Convert.ToSAM_SpaceSystemResult</c> - and <c>ISystemZone</c> declares no <c>GUID</c>, so that binds to
    /// <c>ISystemComponent.GUID</c>: <b>the TPD component's own guid</b>. Nothing in this codebase establishes
    /// that the two are equal, and step 8's "verified on the real run" evidence covers only the TSD route. If
    /// they differ, identity never engages, the whole model falls to name mode, and a block of flats whose rooms
    /// are all called "Bedroom 2" is <b>entirely refused</b> - which is safe but useless. The refusal below says
    /// so once, loudly, rather than as one baffling message per room.
    /// </para>
    /// <para>
    /// <b>The likely correct bridge is <c>IZoneLoad.GUID</c>, not the component guid.</b>
    /// <c>ITSDData</c> - the object carrying <c>TBDPath</c> and <c>TSDPath</c> - exposes
    /// <c>GetZoneLoadForGuid(string)</c>, so a zone load is addressable by the guid the TBD/TSD side uses. Moving
    /// <c>ToSAM_SpaceSystemResult</c> onto <c>zoneLoad.GUID</c> is therefore the probable fix, but it is <b>not a
    /// drive-by</b>: <c>Reference</c> is how a <c>SystemSpaceResult</c> is correlated to its
    /// <c>SystemSpace</c>/<c>SystemZone</c> elsewhere in the energy-centre model and is persisted in JSON, so it
    /// needs checking against a real TPD first. <b>Verify on a real validation model before changing it.</b>
    /// </para>
    /// <para>
    /// <b>Free of TAS COM types</b>, so the preparation can be tested without an installed TAS. The engine
    /// vocabulary it needs - the identity a space carries across the round trip and the two series keys - is
    /// supplied by the caller, the same seam <c>TM59AssessmentCalculator</c> uses for
    /// <c>ResultantTemperatureSeriesKey</c>.
    /// </para>
    /// </summary>
    public class ApproximateResultantTemperatureMap
    {
        private readonly List<string> refusals = new List<string>();

        private readonly List<Space> spaces_Prepared = new List<Space>();

        /// <summary>
        /// Synthesises the approximate <c>ResultantTemperature</c> onto the simulation model's spaces.
        /// </summary>
        /// <param name="analyticalModel_Simulation">
        /// The model read back from the TSD beside the TPD, carrying the mean radiant temperature series. Not
        /// modified: a new model is returned by <see cref="AnalyticalModel"/>.
        /// </param>
        /// <param name="systemSpaceResults">The TPD's per-zone results, carrying the zone temperature series.</param>
        /// <param name="func_StableKey">
        /// The engine identity a space carries across the round trip, matched against each result's
        /// <c>Reference</c>. For TAS the caller passes <c>Analytical.Tas.Query.SimulationSpaceKey</c>. Null falls
        /// the whole map back to unique names, which refuses duplicates rather than guessing between them.
        /// </param>
        /// <param name="seriesKey_MeanRadiantTemperature">The key the TSD conversion wrote the radiant series under.</param>
        /// <param name="seriesKey_ResultantTemperature">The key the assessment will read the synthesised series from.</param>
        public ApproximateResultantTemperatureMap(AnalyticalModel analyticalModel_Simulation, IEnumerable<SystemSpaceResult> systemSpaceResults, Func<Space, string> func_StableKey, string seriesKey_MeanRadiantTemperature, string seriesKey_ResultantTemperature)
        {
            if (analyticalModel_Simulation == null)
            {
                refusals.Add("No simulation model was supplied, so there is nothing to prepare for assessment.");
                return;
            }

            if (string.IsNullOrWhiteSpace(seriesKey_MeanRadiantTemperature) || string.IsNullOrWhiteSpace(seriesKey_ResultantTemperature))
            {
                refusals.Add("The mean radiant temperature and resultant temperature series keys are both required: without them the preparation would silently write a series the assessment does not read.");
                return;
            }

            List<Space> spaces = analyticalModel_Simulation.GetSpaces();
            if (spaces == null || spaces.Count == 0)
            {
                refusals.Add("The simulation model carries no spaces, so there is nothing to prepare for assessment.");
                AnalyticalModel = analyticalModel_Simulation;
                return;
            }

            Dictionary<string, List<SystemSpaceResult>> dictionary_Reference = Group(systemSpaceResults, x => x.Reference);
            Dictionary<string, List<SystemSpaceResult>> dictionary_Name = Group(systemSpaceResults, x => x.Name);

            //Whether identity is in use is decided ONCE, for the whole model, and not per space.
            //
            //A space's engine identity and a TPD result's Reference are stamped by different halves of TAS, so a
            //model where they do not correspond at all is a legitimate shape - and the only thing left there is
            //the name, which is what this route has always used. But deciding that space by space would be
            //actively dangerous: a stamped space whose identity simply has no result would fall through to the
            //name rule and collect a SAME-NAMED sibling's system results. Three flats, one "Bedroom 2", and the
            //flat with no result silently reports its neighbour's. So either every match is by identity or every
            //match is by name, and in name mode duplicates are refused rather than guessed between.
            bool identity = false;
            foreach (Space space in spaces)
            {
                string key = func_StableKey == null ? null : func_StableKey.Invoke(space);
                if (!string.IsNullOrWhiteSpace(key) && dictionary_Reference.ContainsKey(key))
                {
                    identity = true;
                    break;
                }
            }

            if (!identity)
            {
                //Said ONCE, before the per-space refusals, because the per-space ones ("matches 3 TPD results by
                //name") describe the symptom and this describes the cause. See the class remarks: the two guid
                //namespaces have never been shown to agree, and if they do not this is the message that says so.
                refusals.Add("No TPD result identity matched any space identity, so results are being attributed by NAME for the whole model. Rooms sharing a name cannot then be told apart and are refused. If this model has duplicate room names, expect no results: the TPD result reference and the TBD/TSD zone guid are not the same identity.");
            }

            //Two spaces claiming one identity is refused in BOTH directions, which is the rule SimulationSpaceMap
            //already follows: backwards, "which result belongs to this room" has two answers. Refusing only the
            //two-results-to-one-space direction would have left this the one identity consumer in the programme
            //with a weaker policy than the rest.
            HashSet<string> keys_Contested = new HashSet<string>();
            if (identity)
            {
                HashSet<string> keys_Seen = new HashSet<string>();

                foreach (Space space in spaces)
                {
                    string key = func_StableKey == null ? null : func_StableKey.Invoke(space);
                    if (!string.IsNullOrWhiteSpace(key) && !keys_Seen.Add(key))
                    {
                        keys_Contested.Add(key);
                    }
                }
            }

            AdjacencyCluster adjacencyCluster = analyticalModel_Simulation.AdjacencyCluster;

            foreach (Space space in spaces)
            {
                SystemSpaceResult systemSpaceResult = Resolve(space, func_StableKey, dictionary_Reference, dictionary_Name, identity, keys_Contested);
                if (systemSpaceResult == null)
                {
                    continue;
                }

                Space space_Prepared = Synthesise(space, systemSpaceResult, seriesKey_MeanRadiantTemperature, seriesKey_ResultantTemperature);
                if (space_Prepared == null)
                {
                    continue;
                }

                adjacencyCluster.AddObject(space_Prepared);
                spaces_Prepared.Add(space_Prepared);
            }

            AnalyticalModel = new AnalyticalModel(analyticalModel_Simulation, adjacencyCluster);
        }

        /// <summary>
        /// The prepared model - the simulation model with a synthesised <c>ResultantTemperature</c> on every space
        /// that could be resolved and read. <b>This is the model the common assessment runs on</b>, and the point
        /// at which this route stops being distinguishable from any other: from here on it is an
        /// <c>AnalyticalModel</c> carrying the required hourly series, nothing more.
        /// </summary>
        public AnalyticalModel AnalyticalModel { get; }

        /// <summary>
        /// Spaces that received a synthesised series. A space absent from here produced no assessment, and the
        /// reason is in <see cref="Refusals"/>.
        /// </summary>
        public List<Space> Prepared => new List<Space>(spaces_Prepared);

        /// <summary>
        /// Whether the preparation produced a model the assessment can run on. <b>False means refuse</b> - and it
        /// is false on exactly the branches where <see cref="AnalyticalModel"/> is null, so a caller that checks
        /// this cannot then dereference nothing. Mirrors
        /// <see cref="ResultantTemperaturePreparation.IsSupported"/>, so the two preparations are checked the same
        /// way.
        /// <para>
        /// Note this is <b>not</b> "nothing was refused": individual spaces are routinely refused while the model
        /// as a whole is still assessable. It means the preparation itself succeeded.
        /// </para>
        /// </summary>
        public bool IsSupported => AnalyticalModel != null;

        /// <summary>
        /// Every space this preparation refused, and why. <b>Reported, not swallowed</b> - a room missing from the
        /// assessment because its identity could not be resolved is a gap the user has to be able to see.
        /// </summary>
        public List<string> Refusals => new List<string>(refusals);

        /// <summary>
        /// Finds the TPD result belonging to this space: the engine identity first, then a unique name, then
        /// refusal. Never the first same-named candidate.
        /// </summary>
        private SystemSpaceResult Resolve(Space space, Func<Space, string> func_StableKey, Dictionary<string, List<SystemSpaceResult>> dictionary_Reference, Dictionary<string, List<SystemSpaceResult>> dictionary_Name, bool identity, HashSet<string> keys_Contested)
        {
            List<SystemSpaceResult> systemSpaceResults;

            if (identity)
            {
                string key = func_StableKey == null ? null : func_StableKey.Invoke(space);

                if (!string.IsNullOrWhiteSpace(key) && keys_Contested.Contains(key))
                {
                    refusals.Add(string.Format("Space '{0}' shares the identity '{1}' with another space, so no TPD result can be attributed to either of them.", space.Name, key));
                    return null;
                }

                if (string.IsNullOrWhiteSpace(key))
                {
                    refusals.Add(string.Format("Space '{0}' carries no identity while the rest of the model is matched by identity, so no TPD result can be attributed to it.", space.Name));
                    return null;
                }

                if (!dictionary_Reference.TryGetValue(key, out systemSpaceResults))
                {
                    refusals.Add(string.Format("Space '{0}' has no TPD result for its identity '{1}', so it produces no approximate resultant temperature.", space.Name, key));
                    return null;
                }

                if (systemSpaceResults.Count > 1)
                {
                    //One identity on two results is a broken model, not a naming coincidence.
                    refusals.Add(string.Format("Space '{0}' matches {1} TPD results by identity '{2}', so no result can be attributed to it.", space.Name, systemSpaceResults.Count, key));
                    return null;
                }

                return systemSpaceResults[0];
            }

            if (dictionary_Name.TryGetValue(space.Name ?? string.Empty, out systemSpaceResults))
            {
                if (systemSpaceResults.Count == 1)
                {
                    return systemSpaceResults[0];
                }

                //Two results share this name - typically the same room in two flats. Refusing here is the whole
                //reason this route stopped matching by name.
                refusals.Add(string.Format("Space '{0}' matches {1} TPD results by name and carries no identity that tells them apart, so no result can be attributed to it.", space.Name, systemSpaceResults.Count));
                return null;
            }

            refusals.Add(string.Format("Space '{0}' has no TPD result, so it produces no approximate resultant temperature.", space.Name));
            return null;
        }

        /// <summary>
        /// Writes the synthesised series onto a copy of the space, or refuses where either input series is
        /// missing or the two disagree on length.
        /// </summary>
        private Space Synthesise(Space space, SystemSpaceResult systemSpaceResult, string seriesKey_MeanRadiantTemperature, string seriesKey_ResultantTemperature)
        {
            //Asked for ONCE: GetParameterSets deep-clones every set on every call, and these sets carry
            //8760-element series.
            List<ParameterSet> parameterSets = space.GetParameterSets();

            ParameterSet parameterSet = parameterSets == null ? null : parameterSets.Find(x => x.Contains(seriesKey_MeanRadiantTemperature));
            if (parameterSet == null)
            {
                refusals.Add(string.Format("Space '{0}' carries no '{1}' series, so no approximate resultant temperature can be synthesised for it.", space.Name, seriesKey_MeanRadiantTemperature));
                return null;
            }

            JsonArray jsonArray_MeanRadiantTemperature = parameterSet.ToObject(seriesKey_MeanRadiantTemperature) as JsonArray;
            if (jsonArray_MeanRadiantTemperature == null || jsonArray_MeanRadiantTemperature.Count == 0)
            {
                refusals.Add(string.Format("Space '{0}' carries an empty '{1}' series, so no approximate resultant temperature can be synthesised for it.", space.Name, seriesKey_MeanRadiantTemperature));
                return null;
            }

            IndexedDoubles indexedDoubles = systemSpaceResult[SpaceDataType.ZoneTemperature.ToString()];
            if (indexedDoubles == null)
            {
                refusals.Add(string.Format("The TPD result for space '{0}' carries no zone temperature, so no approximate resultant temperature can be synthesised for it.", space.Name));
                return null;
            }

            //The two series are read hour-for-hour, so BOTH their length and their ALIGNMENT have to hold.
            //
            //Neither is provable from what GetValues returns. A bounded read always hands back exactly as many
            //values as were asked for: it WRAPS the requested index into [GetMinIndex(), GetMaxIndex()] rather
            //than running out. So a ten-hour TPD result silently stretches over the year, and - because
            //Create.IndexedDoubles indexes from the TPD's own start hour minus one - a result for a sub-period
            //such as 1 May to 30 September starts at a non-zero index, where asking for index 0 returns some hour
            //in the middle of the period. That is a ROTATED series, and averaging it against the radiant series
            //would fabricate a resultant temperature that looks entirely plausible.
            int? index_Min = indexedDoubles.GetMinIndex();
            int? index_Max = indexedDoubles.GetMaxIndex();

            if (!index_Min.HasValue || !index_Max.HasValue)
            {
                refusals.Add(string.Format("Space '{0}' produced no readable TPD zone temperature series.", space.Name));
                return null;
            }

            if (index_Min.Value != 0)
            {
                refusals.Add(string.Format("Space '{0}' has a TPD zone temperature series starting at hour {1} rather than hour 0, so it cannot be aligned with the radiant temperature series. A TPD simulated over part of the year cannot be combined with a full-year result.", space.Name, index_Min.Value));
                return null;
            }

            //Count, not span, and it must EQUAL the radiant length, not merely reach it: a gapped series
            //has a wide span but missing keys, and a bounded read fills those with default(double) -
            //averaging 0 degrees into the answer - while a longer zone series describes a different period
            //than the radiant one and cannot be aligned to it. Both are refusals.
            if (indexedDoubles.Count != jsonArray_MeanRadiantTemperature.Count)
            {
                refusals.Add(string.Format("Space '{0}' has {1} radiant temperature values but its TPD result holds {2}, so the two cannot be combined.", space.Name, jsonArray_MeanRadiantTemperature.Count, indexedDoubles.Count));
                return null;
            }

            //With the count equal and the minimum fixed at 0, a maximum below Count - 1 is exactly a gap -
            //a missing key the bounded read would zero-fill - and a maximum above it cannot happen with
            //unique keys, but the equality check makes the span exact in both directions.
            if (index_Max.Value != jsonArray_MeanRadiantTemperature.Count - 1)
            {
                refusals.Add(string.Format("Space '{0}' has {1} radiant temperature values but its TPD zone temperature series only reaches hour {2}, so the two cannot be aligned.", space.Name, jsonArray_MeanRadiantTemperature.Count, index_Max.Value));
                return null;
            }

            List<double> values_ZoneTemperature = indexedDoubles.GetValues(new Range<int>(0, jsonArray_MeanRadiantTemperature.Count - 1), true);
            if (values_ZoneTemperature == null || values_ZoneTemperature.Count < jsonArray_MeanRadiantTemperature.Count)
            {
                refusals.Add(string.Format("Space '{0}' produced no readable TPD zone temperature series.", space.Name));
                return null;
            }

            JsonArray jsonArray_ResultantTemperature = new JsonArray();
            for (int i = 0; i < jsonArray_MeanRadiantTemperature.Count; i++)
            {
                //A null or non-numeric element must be REFUSED, and this line used to only half do it. The
                //comment claimed the cast was guarded; it guarded the null and left `GetValue<double>()` to
                //throw on anything else - a string, a bool - which on this route is an exception out of a
                //Grasshopper component instead of the per-space refusal the rest of this method is built on,
                //and it aborts the whole preparation map rather than excluding the one affected space. The
                //series comes from parameter-set data and is not strongly typed, so one hand-edited value
                //could do it.
                if (!TryGetDouble(jsonArray_MeanRadiantTemperature[i], out double value_MeanRadiantTemperature))
                {
                    refusals.Add(string.Format("Space '{0}' has a missing or non-numeric '{1}' value at hour {2}, so no approximate resultant temperature can be synthesised for it.", space.Name, seriesKey_MeanRadiantTemperature, i));
                    return null;
                }

                jsonArray_ResultantTemperature.Add((value_MeanRadiantTemperature + values_ZoneTemperature[i]) / 2);
            }

            Space result = new Space(space);

            //parameterSet is already a private clone - GetParameterSets cloned it - so it is written to directly.
            parameterSet.Add(seriesKey_ResultantTemperature, jsonArray_ResultantTemperature);
            result.Add(parameterSet);

            return result;
        }

        /// <summary>
        /// One hour of a series as a number, or false where the node is absent, null, or not a JSON number.
        /// <para>
        /// <b>Numeric-kind first, then the value - because <c>TryGetValue&lt;double&gt;</c> alone would refuse
        /// perfectly good data.</b> A <c>JsonValue</c> built in-process from an <c>int</c>, <c>float</c> or
        /// <c>decimal</c> reports <c>JsonValueKind.Number</c> and still fails both <c>TryGetValue&lt;double&gt;</c>
        /// and <c>GetValue&lt;double&gt;</c>: the framework converts between numeric types only for values
        /// backed by a <c>JsonElement</c>, which is what parsing produces. So a parsed <c>5</c> reads as
        /// <c>5.0</c> while an in-process <c>5</c> throws - and this series arrives either way, parsed from a
        /// stored parameter set or written directly by SAM code.
        /// </para>
        /// <para>
        /// The JSON text is the one representation every backing agrees on, so it is the fallback, parsed
        /// invariantly - a decimal comma would otherwise read "20.5" as 205 on a machine with a European locale.
        /// The result is that a whole-number radiant value now converts instead of throwing, and only a
        /// genuinely non-numeric node is refused.
        /// </para>
        /// </summary>
        private static bool TryGetDouble(JsonNode jsonNode, out double value)
        {
            value = default;

            if (!(jsonNode is JsonValue jsonValue) || jsonNode.GetValueKind() != JsonValueKind.Number)
            {
                return false;
            }

            if (jsonValue.TryGetValue(out double value_Double))
            {
                value = value_Double;

                return true;
            }

            return double.TryParse(jsonValue.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static Dictionary<string, List<SystemSpaceResult>> Group(IEnumerable<SystemSpaceResult> systemSpaceResults, Func<SystemSpaceResult, string> func)
        {
            Dictionary<string, List<SystemSpaceResult>> result = new Dictionary<string, List<SystemSpaceResult>>();

            foreach (SystemSpaceResult systemSpaceResult in systemSpaceResults ?? new List<SystemSpaceResult>())
            {
                if (systemSpaceResult == null)
                {
                    continue;
                }

                string key = func.Invoke(systemSpaceResult);
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                List<SystemSpaceResult> systemSpaceResults_Key;
                if (!result.TryGetValue(key, out systemSpaceResults_Key))
                {
                    systemSpaceResults_Key = new List<SystemSpaceResult>();
                    result[key] = systemSpaceResults_Key;
                }

                systemSpaceResults_Key.Add(systemSpaceResult);
            }

            return result;
        }
    }
}
