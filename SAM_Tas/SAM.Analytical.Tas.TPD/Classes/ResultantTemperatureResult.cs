// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// One room's hourly <c>ResultantTemperature</c>, keyed by the analytical room and tied to the TAS zone it
    /// was read off.
    /// <para>
    /// <b>Provider-neutral.</b> Nothing here says how the series was obtained - by the current thermostat
    /// bridge or by a future native TAS Systems result - so a consumer of it never has to know. What it does
    /// carry is the zone the series came off, so a completeness check can compare what was read against what
    /// was asked for.
    /// </para>
    /// <para>Immutable; free of TAS COM types.</para>
    /// </summary>
    public class ResultantTemperatureResult
    {
        private readonly IndexedDoubles indexedDoubles;

        public ResultantTemperatureResult(Guid guid_Space, string reference_Zone, int startHour, int endHour, IndexedDoubles indexedDoubles, string diagnostic)
        {
            Guid_Space = guid_Space;
            Reference_Zone = reference_Zone;
            StartHour = startHour;
            EndHour = endHour;
            Diagnostic = diagnostic;

            this.indexedDoubles = indexedDoubles == null ? null : new IndexedDoubles(indexedDoubles);
        }

        /// <summary>The analytical room. The key every consumer uses.</summary>
        public Guid Guid_Space { get; }

        /// <summary>The TAS building-zone guid the series was read off.</summary>
        public string Reference_Zone { get; }

        /// <summary>0-based first hour of the period.</summary>
        public int StartHour { get; }

        /// <summary>0-based last hour of the period, inclusive.</summary>
        public int EndHour { get; }

        /// <summary>What TAS said when the series could not be read. Null on success; never swallowed.</summary>
        public string Diagnostic { get; }

        /// <summary>How many values the period needs.</summary>
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
        /// The series, or null. A defensive COPY on every access - walk a period with
        /// <see cref="TryGetValue"/> instead.
        /// </summary>
        public IndexedDoubles Values
        {
            get { return indexedDoubles == null ? null : new IndexedDoubles(indexedDoubles); }
        }

        /// <summary>One hour's value without copying the series. An absent hour answers NaN, never 0.</summary>
        public bool TryGetValue(int hour, out double value)
        {
            value = double.NaN;

            if (indexedDoubles == null || !indexedDoubles.TryGetValue(hour, out double value_Temp))
            {
                return false;
            }

            value = value_Temp;

            return true;
        }

        /// <summary>Why this series is not usable, or null when it is. One sentence, naming the room.</summary>
        public string Refusal()
        {
            if (Guid_Space == Guid.Empty)
            {
                return "A resultant temperature series was read for no analytical room.";
            }

            if (!string.IsNullOrWhiteSpace(Diagnostic))
            {
                return string.Format("Room {0}: {1}", Guid_Space, Diagnostic);
            }

            string refusal = Query.HourlySeriesRefusal(indexedDoubles, StartHour, EndHour, "resultant temperature");

            return refusal == null ? null : string.Format("Room {0}: {1}", Guid_Space, refusal);
        }

        /// <summary>Whether the series is complete: every hour present, every value finite.</summary>
        public bool IsComplete
        {
            get { return Refusal() == null; }
        }

        public override string ToString()
        {
            return string.Format(
                "Room {0}: {1}/{2} resultant temperatures for hours {3}..{4} from zone {5}{6}",
                Guid_Space,
                Count,
                ExpectedCount,
                StartHour,
                EndHour,
                Reference_Zone ?? "<none>",
                IsComplete ? string.Empty : string.Concat(" - ", Refusal()));
        }
    }
}
