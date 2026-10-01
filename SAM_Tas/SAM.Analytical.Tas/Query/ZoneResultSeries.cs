// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TSD;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The hourly values of several zone arrays for many zones, read DAY BY DAY across all of them: every zone's
        /// every array for one day, then the next day. The values are the same <c>GetDailyZoneResult</c> values a
        /// zone-by-zone read returns; only the order they are asked for differs.
        /// <para>
        /// Why the order matters: TSD.exe decodes its results one whole simulated day at a time and keeps decoded days
        /// in a cache of roughly 550 MB. Once a project's decoded year no longer fits - measured: a 161 MB TSD with 90
        /// zones fits, a 483 MB TSD with 270 zones does not - any read that walks the whole year for ONE zone (365
        /// <c>GetDailyZoneResult</c> calls, or one <c>GetAnnualZoneResult</c>) evicts the days it needs next and
        /// decodes the whole year again: ~8 s per zone array on the 483 MB file, against ~40 ms per zone array read
        /// day by day. (the SAM_Tas PR record.)
        /// </para>
        /// </summary>
        /// <param name="zoneDatas">The zones to read.</param>
        /// <param name="index_Start">First day, 1-based, inclusive.</param>
        /// <param name="index_End">Last day, 1-based, inclusive.</param>
        /// <param name="tsdZoneArrays">The arrays to read for every zone.</param>
        /// <returns>
        /// One entry per zone, in the order given (null for a null zone): each array's values as an 8760-hour array,
        /// hour <c>(day - 1) * 24 + h</c>, zero outside the days read - the layout <see cref="Overheating(ZoneData, int, int, double)"/>
        /// has always built.
        /// </returns>
        public static List<Dictionary<tsdZoneArray, float[]>> ZoneResultSeries(this IList<ZoneData> zoneDatas, int index_Start, int index_End, IEnumerable<tsdZoneArray> tsdZoneArrays)
        {
            if (zoneDatas == null || tsdZoneArrays == null)
            {
                return null;
            }

            List<tsdZoneArray> tsdZoneArrays_Unique = tsdZoneArrays.Distinct().ToList();

            List<Dictionary<tsdZoneArray, float[]>> result = new List<Dictionary<tsdZoneArray, float[]>>(zoneDatas.Count);
            foreach (ZoneData zoneData in zoneDatas)
            {
                if (zoneData == null)
                {
                    result.Add(null);
                    continue;
                }

                Dictionary<tsdZoneArray, float[]> dictionary = new Dictionary<tsdZoneArray, float[]>();
                foreach (tsdZoneArray tsdZoneArray in tsdZoneArrays_Unique)
                {
                    dictionary[tsdZoneArray] = new float[8760];
                }

                result.Add(dictionary);
            }

            for (int i = index_Start; i <= index_End; i++)
            {
                int startHour = (i * 24) - 24;

                for (int j = 0; j < zoneDatas.Count; j++)
                {
                    Dictionary<tsdZoneArray, float[]> dictionary = result[j];
                    if (dictionary == null)
                    {
                        continue;
                    }

                    foreach (tsdZoneArray tsdZoneArray in tsdZoneArrays_Unique)
                    {
                        float[] yearlyValues = dictionary[tsdZoneArray];
                        float[] dailyValues = (zoneDatas[j].GetDailyZoneResult(i, (short)tsdZoneArray) as IEnumerable).Cast<float>().ToArray();
                        int counter = 0;
                        for (int n = startHour; n <= startHour + 23; n++)
                        {
                            yearlyValues[n] = dailyValues[counter];
                            counter += 1;
                        }
                    }
                }
            }

            return result;
        }
    }
}
