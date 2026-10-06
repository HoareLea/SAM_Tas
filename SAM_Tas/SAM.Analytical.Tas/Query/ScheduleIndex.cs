// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The index of the first schedule in <paramref name="existingValues"/> whose 24 values equal
        /// <paramref name="values"/>, or -1 if none does.
        /// <para>
        /// <b>Identity is the values, never the name.</b> A TBD schedule is building-level and shared by
        /// apertures across every zone, so a Part O window, an internal door and a security-restricted
        /// opening that all need <c>000000001111111111111110</c> should reference one schedule however
        /// differently they name it. Matching on name instead is what produced
        /// <c>PartO_DayOpen_08_23 (1)</c>, <c>(2)</c>... on repeated export.
        /// </para>
        /// <para>
        /// First match wins, so the result is stable for a given building ordering and the operation is
        /// idempotent: a second export finds the schedule the first one created.
        /// </para>
        /// </summary>
        public static int ScheduleIndex(IEnumerable<int[]> existingValues, IEnumerable<int> values)
        {
            if (existingValues == null || values == null)
            {
                return -1;
            }

            int[] requested = values.ToArray();
            if (requested.Length != ScheduleHourCount)
            {
                return -1;
            }

            int index = 0;
            foreach (int[] existing in existingValues)
            {
                if (ScheduleValuesEqual(existing, requested))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}
