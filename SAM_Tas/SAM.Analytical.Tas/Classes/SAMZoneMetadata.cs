// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>The SAM-only section of a TBD zone description</b> - the authored SAM airflow REQUIREMENT, carried
    /// across the <c>SAM -> TBD -> SAM</c> seam that TAS itself cannot represent.
    /// <para>
    /// SAM lets an engineer state a supply-air requirement on four simultaneous bases -
    /// <c>SupplyAirFlow</c> [m3/s], <c>SupplyAirFlowPerArea</c> [m3/s/m2], <c>SupplyAirFlowPerPerson</c>
    /// [m3/s/p] and <c>SupplyAirChangesPerHour</c> [ACH] - which
    /// <see cref="Analytical.Query.CalculatedSupplyAirFlow(Space)"/> SUMS into one required total. TAS has no
    /// field for that decomposition: at most it holds the per-person rate (<c>InternalGain.freshAirRate</c>)
    /// and, when a Ventilation profile deliberately realises the requirement, one air-change rate
    /// (<c>ticV.factor</c>). Writing the summed total into that single ACH slot and reading it back as the ACH
    /// BASIS is what made the figure grow by the per-person term on every round trip. Preserving the authored
    /// bases here removes the feedback without changing the physical total.
    /// </para>
    /// <para>
    /// <b>Where it lives.</b> One <c>"; "</c>-separated segment of <c>TBD.zone.description</c>, the string the
    /// exporter already uses for SAM-only information (<c>[Id]</c>, <c>[LevelName]</c>):
    /// <code>
    /// [Id]=1234; [LevelName]=Level 01; [SAM_META_V1]={"ventilation":{...},"native":{...}}
    /// </code>
    /// Deliberately NOT <c>InternalCondition.description</c> - that carries the NCM activity name and has real
    /// TAS meaning, which the import already preserves.
    /// </para>
    /// <para>
    /// <b>Versioned, and the only place the section is read or written.</b> The marker carries the version, so
    /// a reader that does not recognise a section leaves the requirement to the native TAS import rather than
    /// guessing. <see cref="Compose"/> and <see cref="Parse"/> are the single parser/writer pair -
    /// no call site assembles or picks apart this string itself. The payload is a JSON object so a further
    /// section (exhaust is the obvious next one) can be added beside <c>"ventilation"</c> without a new version.
    /// </para>
    /// <para>
    /// <b>Derived geometry is deliberately absent.</b> Area and volume already belong to the TBD zone; storing
    /// them here would create a second, staler copy of something TAS states authoritatively.
    /// </para>
    /// </summary>
    public class SAMZoneMetadata
    {
        /// <summary>The versioned marker this class owns, including the <c>=</c>.</summary>
        public const string Marker = "[SAM_META_V1]=";

        /// <summary>
        /// Any version of the marker. <see cref="Compose"/> drops every segment matching this before writing
        /// its own, so a file written by a future version is REPLACED rather than left beside a second,
        /// contradicting section.
        /// </summary>
        private const string Marker_AnyVersion = "[SAM_META_";

        private const string Segment_Id = "[Id]=";
        private const string Segment_LevelName = "[LevelName]=";
        private const char Separator = ';';

        /// <summary>Authored <c>InternalConditionParameter.SupplyAirFlow</c> [m3/s], or NaN when not stated.</summary>
        public double SupplyAirFlow { get; set; } = double.NaN;

        /// <summary>Authored <c>InternalConditionParameter.SupplyAirFlowPerArea</c> [m3/s/m2], or NaN.</summary>
        public double SupplyAirFlowPerArea { get; set; } = double.NaN;

        /// <summary>Authored <c>InternalConditionParameter.SupplyAirFlowPerPerson</c> [m3/s/p], or NaN.</summary>
        public double SupplyAirFlowPerPerson { get; set; } = double.NaN;

        /// <summary>Authored <c>InternalConditionParameter.SupplyAirChangesPerHour</c> [ACH], or NaN.</summary>
        public double SupplyAirChangesPerHour { get; set; } = double.NaN;

        /// <summary>
        /// Whether a SAM Ventilation profile was assigned when this was exported - i.e. whether the engineer
        /// deliberately chose TBD Building Simulator mechanical ventilation as the REALISATION of the
        /// requirement above. False means the four values are requirement data only and no <c>ticV</c> rate
        /// was authored by SAM; the import must not activate one on their behalf.
        /// </summary>
        public bool VentilationProfileApplied { get; set; }

        /// <summary>
        /// <c>InternalGain.freshAirRate</c> [l/s/p] as the export left it - a FINGERPRINT, not a requirement.
        /// If TAS no longer states this, the file was edited after SAM wrote it and the authored bases above
        /// are no longer known to describe it.
        /// </summary>
        public double FreshAirRate { get; set; } = double.NaN;

        /// <summary>
        /// <c>ticV.factor</c> [ACH] as the export left it, and only when
        /// <see cref="VentilationProfileApplied"/> - the same fingerprint role as <see cref="FreshAirRate"/>.
        /// Where SAM never authored the factor there is nothing to fingerprint, and a TAS user's own value
        /// there is not evidence of anything having gone stale.
        /// </summary>
        public double VentilationFactor { get; set; } = double.NaN;

        /// <summary>
        /// The SAM section of <paramref name="zoneDescription"/>, or <c>null</c> when there is none, the
        /// version is not this one, or the payload does not parse. A null answer always means "fall back to
        /// what TAS itself states" - never "the requirement is empty".
        /// </summary>
        public static SAMZoneMetadata Parse(string zoneDescription)
        {
            foreach (string segment in Segments(zoneDescription))
            {
                if (!segment.StartsWith(Marker, StringComparison.Ordinal))
                {
                    continue;
                }

                return FromJson(segment.Substring(Marker.Length));
            }

            return null;
        }

        /// <summary>
        /// The complete zone description to write: the managed segments in a fixed order, then every segment
        /// of <paramref name="zoneDescription"/> this class does not own, then the SAM section last.
        /// <para>
        /// Unrecognised content is preserved verbatim and in its original order - a TAS user's own note in the
        /// zone description survives an export, which the previous unconditional overwrite did not allow.
        /// </para>
        /// <para>
        /// <c>[Id]</c> and <c>[LevelName]</c> are rewritten from the space where it states them, and otherwise
        /// KEPT from the existing description. The import does not read either back onto the space, so a model
        /// that has been through <c>FromTBD</c> no longer carries them - without the fallback the second
        /// generation would silently drop a level name the first one wrote. They used to survive only by
        /// accident, because a space stating neither left the description untouched; now that the SAM section
        /// is always written, that accident is gone and the intent has to be stated.
        /// </para>
        /// </summary>
        /// <returns>The description, or <c>null</c> when there is nothing at all to write (leave the existing one alone).</returns>
        public static string Compose(string zoneDescription, string id, string levelName, SAMZoneMetadata metadata)
        {
            List<string> values = new List<string>();

            //TODO: Update [Id] to [Element Id]
            string value = string.IsNullOrWhiteSpace(id) ? Existing(zoneDescription, Segment_Id) : id;
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(Segment_Id + value);
            }

            //TODO: Update [LevelName] to [Level Name]
            value = string.IsNullOrWhiteSpace(levelName) ? Existing(zoneDescription, Segment_LevelName) : levelName;
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(Segment_LevelName + value);
            }

            foreach (string segment in Segments(zoneDescription))
            {
                if (Managed(segment))
                {
                    continue;
                }

                values.Add(segment);
            }

            if (metadata != null)
            {
                string json = metadata.ToJson();

                //The payload holds numbers and fixed identifiers only, so it cannot contain the separator.
                //Refusing rather than writing an unparseable description keeps the guarantee one-directional:
                //a section that is present always round-trips.
                if (!string.IsNullOrEmpty(json) && json.IndexOf(Separator) < 0)
                {
                    values.Add(Marker + json);
                }
            }

            return values.Count == 0 ? null : string.Join("; ", values);
        }

        /// <summary>The payload, without the marker. Deterministic: fixed key order, invariant-culture numbers.</summary>
        public string ToJson()
        {
            JsonObject jsonObject_Ventilation = new JsonObject();
            Write(jsonObject_Ventilation, "flow", SupplyAirFlow);
            Write(jsonObject_Ventilation, "flowPerArea", SupplyAirFlowPerArea);
            Write(jsonObject_Ventilation, "flowPerPerson", SupplyAirFlowPerPerson);
            Write(jsonObject_Ventilation, "airChangesPerHour", SupplyAirChangesPerHour);
            jsonObject_Ventilation["profile"] = JsonValue.Create(VentilationProfileApplied);

            JsonObject jsonObject_Native = new JsonObject();
            Write(jsonObject_Native, "freshAirRate", FreshAirRate);
            Write(jsonObject_Native, "ticV", VentilationFactor);

            JsonObject jsonObject = new JsonObject
            {
                ["ventilation"] = jsonObject_Ventilation,
                ["native"] = jsonObject_Native,
            };

            return jsonObject.ToJsonString();
        }

        private static SAMZoneMetadata FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            JsonObject jsonObject = null;
            try
            {
                jsonObject = JsonNode.Parse(json) as JsonObject;
            }
            catch (Exception)
            {
                //Malformed transport data is not an error to raise here - it is a reason to trust TAS instead.
                return null;
            }

            if (jsonObject == null)
            {
                return null;
            }

            JsonObject jsonObject_Ventilation = Object(jsonObject, "ventilation");
            if (jsonObject_Ventilation == null)
            {
                return null;
            }

            JsonObject jsonObject_Native = Object(jsonObject, "native");

            return new SAMZoneMetadata
            {
                SupplyAirFlow = Read(jsonObject_Ventilation, "flow"),
                SupplyAirFlowPerArea = Read(jsonObject_Ventilation, "flowPerArea"),
                SupplyAirFlowPerPerson = Read(jsonObject_Ventilation, "flowPerPerson"),
                SupplyAirChangesPerHour = Read(jsonObject_Ventilation, "airChangesPerHour"),
                VentilationProfileApplied = ReadBoolean(jsonObject_Ventilation, "profile"),
                FreshAirRate = Read(jsonObject_Native, "freshAirRate"),
                VentilationFactor = Read(jsonObject_Native, "ticV"),
            };
        }

        /// <summary>The description's segments, trimmed, empties dropped. Never null.</summary>
        private static IEnumerable<string> Segments(string zoneDescription)
        {
            if (string.IsNullOrWhiteSpace(zoneDescription))
            {
                yield break;
            }

            foreach (string segment in zoneDescription.Split(Separator))
            {
                string segment_Trimmed = segment?.Trim();
                if (string.IsNullOrEmpty(segment_Trimmed))
                {
                    continue;
                }

                yield return segment_Trimmed;
            }
        }

        /// <summary>The value an existing description states for one managed segment, or null.</summary>
        private static string Existing(string zoneDescription, string prefix)
        {
            foreach (string segment in Segments(zoneDescription))
            {
                if (segment.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return segment.Substring(prefix.Length).Trim();
                }
            }

            return null;
        }

        /// <summary>Whether <see cref="Compose"/> rewrites this segment from its own inputs rather than preserving it.</summary>
        private static bool Managed(string segment)
        {
            return segment.StartsWith(Segment_Id, StringComparison.Ordinal)
                || segment.StartsWith(Segment_LevelName, StringComparison.Ordinal)
                || segment.StartsWith(Marker_AnyVersion, StringComparison.Ordinal);
        }

        private static void Write(JsonObject jsonObject, string name, double value)
        {
            //Omitted, not written as null: "absent" and "stated as nothing" are the same thing to the four
            //bases, and omitting keeps the description short - it shares a TBD field with everything else.
            if (double.IsNaN(value))
            {
                return;
            }

            jsonObject[name] = JsonValue.Create(value);
        }

        private static JsonObject Object(JsonObject jsonObject, string name)
        {
            if (jsonObject == null || !jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode))
            {
                return null;
            }

            return jsonNode as JsonObject;
        }

        private static double Read(JsonObject jsonObject, string name)
        {
            if (jsonObject == null || !jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode))
            {
                return double.NaN;
            }

            if (jsonNode is JsonValue jsonValue && jsonValue.TryGetValue(out double result))
            {
                return result;
            }

            return double.NaN;
        }

        private static bool ReadBoolean(JsonObject jsonObject, string name)
        {
            if (jsonObject == null || !jsonObject.TryGetPropertyValue(name, out JsonNode jsonNode))
            {
                return false;
            }

            return jsonNode is JsonValue jsonValue && jsonValue.TryGetValue(out bool result) && result;
        }
    }
}
