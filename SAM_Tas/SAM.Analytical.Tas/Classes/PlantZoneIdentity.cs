// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>Which air handling unit a generated TAS plant zone belongs to</b> - the identity that lets
    /// <see cref="Modify.UpdateIZAMs(TBD.TBDDocument, AdjacencyCluster)"/> recognise the plant zone it wrote
    /// last time and update it, instead of appending a second one beside it.
    /// <para>
    /// <b>Why an identity is needed at all.</b> A Part O optimisation round warm starts from the canonical
    /// TBD - a file copy of a conversion that has already run <c>UpdateIZAMs</c> once - and then runs the
    /// ventilation half of the workflow again over that copy. Without a way to find the existing plant zone,
    /// each such round created another 3 x 3 x 2 box per unit and renamed it to the unit, leaving the older
    /// zone behind: same name, its internal condition and inter-zone air movements stripped by the
    /// remove-by-name step that precedes the rebuild, and nothing pointing at it. Three units came back as
    /// six zones, three of them without internal conditions.
    /// </para>
    /// <para>
    /// <b>Why the AHU guid rather than the zone name.</b> The zone is NAMED after the unit, so the name is
    /// the obvious candidate and the wrong one: it is a presentation string, it is not unique - two units
    /// may legitimately carry the same name - and it changes whenever the unit is renamed, which would
    /// orphan the zone and duplicate it on the next round. <see cref="AirHandlingUnit.Guid"/> is the
    /// authoritative identity of the unit in the SAM model and survives both.
    /// </para>
    /// <para>
    /// <b>Where it lives.</b> One <c>"; "</c>-separated segment of <c>TBD.zone.description</c>, the channel
    /// the exporter already uses for SAM-only information, alongside <c>[Id]</c>, <c>[LevelName]</c> and
    /// <see cref="SAMZoneMetadata"/>'s own section:
    /// <code>
    /// [SAM_AHU_V1]=0f8b1c22-6c1a-4f5e-9a3d-1b2c3d4e5f60
    /// </code>
    /// Every segment this class does not own is preserved verbatim, so a plant zone that later gains a
    /// <c>[SAM_META_V1]</c> section - or a TAS user's own note - keeps it.
    /// </para>
    /// <para>
    /// <b>Versioned.</b> The marker carries its version and <see cref="Compose"/> drops any other version of
    /// it before writing this one, so a file written by a future version is replaced rather than left beside
    /// a second, contradicting identity.
    /// </para>
    /// </summary>
    public static class PlantZoneIdentity
    {
        /// <summary>The versioned marker this class owns, including the <c>=</c>.</summary>
        public const string Marker = "[SAM_AHU_V1]=";

        /// <summary>
        /// Any version of the marker. <see cref="Compose"/> drops every segment matching this before writing
        /// its own.
        /// </summary>
        private const string Marker_AnyVersion = "[SAM_AHU_";

        private const char Separator = ';';

        /// <summary>
        /// The complete zone description to write: every segment of <paramref name="zoneDescription"/> this
        /// class does not own, in its original order, then this unit's identity last.
        /// </summary>
        /// <param name="zoneDescription">The description the zone currently carries. May be null or empty.</param>
        /// <param name="guid">The air handling unit's <see cref="SAMObject.Guid"/>.</param>
        /// <returns>
        /// The description to write, or <c>null</c> when <paramref name="guid"/> is empty - there is no
        /// identity to state, so the existing description is left alone rather than rewritten.
        /// </returns>
        public static string Compose(string zoneDescription, Guid guid)
        {
            if (guid == Guid.Empty)
            {
                return null;
            }

            List<string> values = new List<string>();

            foreach (string segment in Segments(zoneDescription))
            {
                if (segment.StartsWith(Marker_AnyVersion, StringComparison.Ordinal))
                {
                    continue;
                }

                values.Add(segment);
            }

            values.Add(Marker + guid.ToString("D"));

            return string.Join("; ", values);
        }

        /// <summary>
        /// The air handling unit stated by <paramref name="zoneDescription"/>, or <see cref="Guid.Empty"/>
        /// when it states none, states a version this build does not recognise, or states something that is
        /// not a guid.
        /// <para>
        /// An empty answer always means "this zone does not claim to belong to a unit" - never "it belongs to
        /// no unit". A plant zone written before this identity existed answers empty, and is adopted by name
        /// instead; see <c>Modify.ResolvePlantZoneReuse</c>.
        /// </para>
        /// </summary>
        public static Guid Parse(string zoneDescription)
        {
            foreach (string segment in Segments(zoneDescription))
            {
                if (!segment.StartsWith(Marker, StringComparison.Ordinal))
                {
                    continue;
                }

                return Guid.TryParse(segment.Substring(Marker.Length).Trim(), out Guid result) ? result : Guid.Empty;
            }

            return Guid.Empty;
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
    }
}
