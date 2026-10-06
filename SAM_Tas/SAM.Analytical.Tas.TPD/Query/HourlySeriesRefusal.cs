// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// Why an hourly series is not a complete answer for a period, or null when it is: one value for
        /// every hour <paramref name="startHour"/>..<paramref name="endHour"/> inclusive, no hour missing,
        /// no hour outside the period, and every value finite.
        /// <para>
        /// <b>Indexed, not merely counted.</b> <c>IndexedDoubles</c> is sparse, so a hole in the middle of
        /// the period and an extra value beyond it can add up to the right total. Both the count and every
        /// index are checked, and nothing is ever padded, truncated or repaired to make a series fit.
        /// </para>
        /// </summary>
        /// <param name="indexedDoubles">The series, keyed by 0-based hour.</param>
        /// <param name="startHour">0-based first hour of the period.</param>
        /// <param name="endHour">0-based last hour of the period, inclusive.</param>
        /// <param name="quantity">What the series is, for the sentence - "resultant temperature".</param>
        public static string HourlySeriesRefusal(IndexedDoubles indexedDoubles, int startHour, int endHour, string quantity)
        {
            if (indexedDoubles == null)
            {
                return string.Format("no {0} series came back.", quantity);
            }

            int count_Expected = endHour - startHour + 1;

            if (count_Expected <= 0)
            {
                return string.Format("the requested period {0}..{1} contains no hours.", startHour, endHour);
            }

            if (indexedDoubles.Count != count_Expected)
            {
                return string.Format(
                    "{0} {1} value(s) came back for hours {2}..{3}, which is {4} hour(s).",
                    indexedDoubles.Count,
                    quantity,
                    startHour,
                    endHour,
                    count_Expected);
            }

            for (int hour = startHour; hour <= endHour; hour++)
            {
                if (!indexedDoubles.TryGetValue(hour, out double value))
                {
                    return string.Format("no {0} at hour {1}.", quantity, hour);
                }

                if (double.IsNaN(value) || double.IsInfinity(value))
                {
                    return string.Format("the {0} at hour {1} is {2}.", quantity, hour, value);
                }
            }

            return null;
        }
    }
}
