// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// One room's <c>ZoneTemperature</c> series, tied to the exact native zone and zone load it was read
    /// from.
    /// <para>
    /// <b>Why the native identities travel with the numbers.</b> A series on its own cannot be checked:
    /// the question the route has to answer is not "are there 8760 values" but "are these 8760 values
    /// the ones belonging to <i>this</i> room's zone". Carrying
    /// <see cref="Reference_SystemZone"/> and <see cref="Reference_ZoneLoad"/> lets the completeness
    /// gate compare what was read against what the room's binding said to read, and refuse if a series
    /// came off the wrong object - which is exactly what a name-keyed lookup would do silently in a file
    /// where two zones are both called "System Zone 1", as a real TAS-authored one was.
    /// </para>
    /// <para>
    /// <b>The values are indexed, not merely counted.</b> <c>IndexedDoubles</c> is a sparse map, so a
    /// hole in the middle of the period is a missing index rather than a short list that happens to have
    /// the right total. Both are checked.
    /// </para>
    /// <para>Free of TAS COM types.</para>
    /// </summary>
    public class SystemZoneTemperatureResult
    {
        private readonly IndexedDoubles indexedDoubles;

        public SystemZoneTemperatureResult(
            Guid guid_Space,
            Guid guid_SystemSpace,
            string reference_SystemZone,
            string reference_ZoneLoad,
            int startHour,
            int endHour,
            IndexedDoubles indexedDoubles,
            string diagnostic)
        {
            Guid_Space = guid_Space;
            Guid_SystemSpace = guid_SystemSpace;
            Reference_SystemZone = reference_SystemZone;
            Reference_ZoneLoad = reference_ZoneLoad;
            StartHour = startHour;
            EndHour = endHour;
            Diagnostic = diagnostic;

            this.indexedDoubles = indexedDoubles == null ? null : new IndexedDoubles(indexedDoubles);
        }

        /// <summary>The analytical room. This is the key PR3 and PR4 use.</summary>
        public Guid Guid_Space { get; }

        /// <summary>PR1's <c>SystemSpace</c> for that room.</summary>
        public Guid Guid_SystemSpace { get; }

        /// <summary>The native zone the series was read off.</summary>
        public string Reference_SystemZone { get; }

        /// <summary>The zone load that zone is bound to.</summary>
        public string Reference_ZoneLoad { get; }

        /// <summary>First hour of the requested period, 0-based.</summary>
        public int StartHour { get; }

        /// <summary>Last hour of the requested period, 0-based and inclusive.</summary>
        public int EndHour { get; }

        /// <summary>
        /// What TAS said when the series could not be read - a COM message, usually. Null on success.
        /// <b>Never swallowed</b>: a result that failed to read says why.
        /// </summary>
        public string Diagnostic { get; }

        /// <summary>
        /// The series, or null where none came back.
        /// <para>
        /// <b>This is a defensive COPY, taken on every access.</b> The record is immutable and handing
        /// out its own collection would let a caller edit a published result. A copy of an annual series
        /// is eight thousand seven hundred and sixty entries, so <b>never call this inside a loop over
        /// the hours</b> - use <see cref="TryGetValue"/>, or take the copy once and walk that. Reading
        /// it per hour is quadratic in the period and turns an instant comparison into minutes.
        /// </para>
        /// </summary>
        public IndexedDoubles Values
        {
            get { return indexedDoubles == null ? null : new IndexedDoubles(indexedDoubles); }
        }

        /// <summary>
        /// One hour's value, without copying the series. This is the accessor to use when walking a
        /// period - see the remarks on <see cref="Values"/> for why that matters.
        /// </summary>
        /// <param name="hour">The 0-based hour.</param>
        public bool TryGetValue(int hour, out double value)
        {
            value = double.NaN;

            //IndexedDoubles.TryGetValue writes default(double) - zero - into its out parameter when the
            //index is absent, so it cannot be passed this one directly: a caller that ignored the bool
            //would read a missing hour as 0 degrees, which is a plausible temperature. An absent hour
            //answers NaN here, which is not.
            if (indexedDoubles == null || !indexedDoubles.TryGetValue(hour, out double value_Temp))
            {
                return false;
            }

            value = value_Temp;

            return true;
        }

        /// <summary>
        /// How many hourly values the request asked for: <c>EndHour - StartHour + 1</c>.
        /// <b>Never a hardcoded 8760</b> - the period is whatever the route requested, and a 24-hour
        /// request that answered 8760 values would be as wrong as an annual one that answered 24.
        /// </summary>
        public int ExpectedCount
        {
            get { return EndHour - StartHour + 1; }
        }

        /// <summary>How many values came back.</summary>
        public int Count
        {
            get { return indexedDoubles == null ? 0 : indexedDoubles.Count; }
        }

        /// <summary>
        /// Whether the series is complete: every hour of the requested period present, and every value
        /// finite. A NaN is not a temperature and an unsimulated hour is not a result.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return Refusal() == null;
            }
        }

        /// <summary>
        /// Why this series is not usable, or null when it is. One sentence, naming the room.
        /// </summary>
        public string Refusal()
        {
            if (Guid_Space == Guid.Empty)
            {
                return "A zone temperature series was read for no analytical room.";
            }

            if (!string.IsNullOrWhiteSpace(Diagnostic))
            {
                return string.Format("Room {0}: {1}", Guid_Space, Diagnostic);
            }

            if (indexedDoubles == null)
            {
                return string.Format(
                    "Room {0}: no zone temperature series came back from native zone {1}.",
                    Guid_Space,
                    Reference_SystemZone ?? "<none>");
            }

            if (ExpectedCount <= 0)
            {
                return string.Format(
                    "Room {0}: the requested period {1}..{2} contains no hours.",
                    Guid_Space,
                    StartHour,
                    EndHour);
            }

            if (indexedDoubles.Count != ExpectedCount)
            {
                return string.Format(
                    "Room {0}: {1} zone temperature value(s) came back for hours {2}..{3}, which is {4} hour(s).",
                    Guid_Space,
                    indexedDoubles.Count,
                    StartHour,
                    EndHour,
                    ExpectedCount);
            }

            for (int i = StartHour; i <= EndHour; i++)
            {
                if (!indexedDoubles.TryGetValue(i, out double value))
                {
                    return string.Format("Room {0}: no zone temperature at hour {1}.", Guid_Space, i);
                }

                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return string.Format("Room {0}: the zone temperature at hour {1} is {2}.", Guid_Space, i, value);
                }
            }

            return null;
        }

        public override string ToString()
        {
            return string.Format(
                "Room {0}: {1}/{2} zone temperatures for hours {3}..{4} from zone {5} / load {6}{7}",
                Guid_Space,
                Count,
                ExpectedCount,
                StartHour,
                EndHour,
                Reference_SystemZone ?? "<none>",
                Reference_ZoneLoad ?? "<none>",
                IsComplete ? string.Empty : string.Concat(" - ", Refusal()));
        }
    }
}
