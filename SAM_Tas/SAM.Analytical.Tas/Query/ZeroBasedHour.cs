// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Converts a TSD hourly index to SAM's 0-based hour of the year - the one place the Tas base is handled.
        /// <para>
        /// TSD hourly results (<c>GetHourlyZoneResult</c>, <c>GetHourlyBuildingResult</c>, the index row of
        /// <c>GetPeakZoneGains</c>) are <b>1-based</b>: index <i>n</i> is the hour (<i>n</i>-1):00 to <i>n</i>:00.
        /// Index 0 is not an hour: Tas answers it with its -1 "no value" sentinel, and a peak of 0 reports index 0.
        /// </para>
        /// </summary>
        /// <returns>0-based hour of the year, or <c>null</c> when <paramref name="hourIndex"/> is not a TSD hour (below 1).</returns>
        public static int? ZeroBasedHourOfYear(int hourIndex)
        {
            return hourIndex < 1 ? null : hourIndex - 1;
        }

        /// <summary>
        /// The 0-based hour of the day (0 = 00:00-01:00) of a 1-based TSD hourly index. Design-day runs sit in the
        /// same day-aligned hourly series, so this holds for a design-day peak as well as an annual one.
        /// </summary>
        public static int? ZeroBasedHourOfDay(int hourIndex)
        {
            return hourIndex < 1 ? null : (hourIndex - 1) % 24;
        }
    }
}
