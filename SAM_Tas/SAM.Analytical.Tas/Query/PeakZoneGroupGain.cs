// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The peak of the hourly sum of several zones' series, reported the way TSD's
        /// <c>BuildingData.GetPeakZoneGroupGains</c> reports it for a full-year simulation - so a caller holding the
        /// series need not ask TSD, which on a large TSD walks the whole year again for every group (see
        /// <see cref="ZoneResultSeries"/>).
        /// <para>
        /// The rule, measured against TSD 2.0.0.1 and matched exactly (value bits and hour) on every group tried - 163
        /// groups on a 90-zone TSD, 103 of them with a positive peak, including a 90-zone group, plus 106 reordered
        /// multi-zone groups: the hourly sum is accumulated in SINGLE precision, zone by zone IN THE ORDER GIVEN; the
        /// peak is the first hour whose sum is strictly larger than zero and than every earlier hour's; the hour is
        /// 1-based. A group with no positive hour peaks at 0 at hour 0.
        /// </para>
        /// </summary>
        /// <param name="values">Each zone's series, all the same length, in the order the group lists the zones.</param>
        /// <param name="index">The 1-based hour of the peak; 0 when no hour is positive.</param>
        /// <param name="max">The peak.</param>
        /// <returns>False, with index -1 and max NaN, when there is nothing to sum or the series differ in length.</returns>
        public static bool TryGetPeakZoneGroupGain(this IList<float[]> values, out int index, out double max)
        {
            index = -1;
            max = double.NaN;

            if (values == null || values.Count == 0 || values[0] == null)
            {
                return false;
            }

            int count = values[0].Length;
            foreach (float[] values_Zone in values)
            {
                if (values_Zone == null || values_Zone.Length != count)
                {
                    return false;
                }
            }

            float peak = 0;
            int index_Peak = 0;
            for (int i = 0; i < count; i++)
            {
                float sum = 0;
                for (int j = 0; j < values.Count; j++)
                {
                    sum += values[j][i];
                }

                if (sum > peak)
                {
                    peak = sum;
                    index_Peak = i + 1;
                }
            }

            index = index_Peak;
            max = peak;
            return true;
        }
    }
}
