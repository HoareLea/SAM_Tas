// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The number of hourly values a TBD schedule is read and written at. <c>TBD.ISchedule</c> exposes
        /// only <c>name</c>, <c>description</c> and a parameterised <c>int values[int]</c> - it carries no
        /// count of its own, so 24 is this side's contract, not the COM interface's.
        /// </summary>
        public const int ScheduleHourCount = 24;

        /// <summary>
        /// The TAS-side representation of a SAM availability schedule: <c>int[24]</c>, 1 for an available
        /// hour and 0 for an unavailable one.
        /// <para>
        /// The seam is deliberately int-valued rather than bool-valued even though
        /// <see cref="DailyAvailabilitySchedule"/> is binary, because the same seam also has to carry the
        /// legacy general-valued <see cref="Profile"/> route without changing what it used to write - see
        /// the <see cref="Profile"/> overload.
        /// </para>
        /// </summary>
        public static int[] ScheduleValues(this DailyAvailabilitySchedule dailyAvailabilitySchedule)
        {
            if (dailyAvailabilitySchedule == null)
            {
                return null;
            }

            bool[] values = dailyAvailabilitySchedule.GetValues();
            if (values == null || values.Length != ScheduleHourCount)
            {
                return null;
            }

            int[] result = new int[ScheduleHourCount];
            for (int hour = 0; hour < ScheduleHourCount; hour++)
            {
                result[hour] = values[hour] ? 1 : 0;
            }

            return result;
        }

        /// <summary>
        /// The TAS-side representation of a legacy <see cref="Profile"/> used as a schedule carrier, or
        /// null when the profile cannot supply 24 hourly values.
        /// <para>
        /// <b>This reproduces the pre-existing conversion exactly</b> - <c>GetDailyValues()</c> followed by
        /// <c>System.Convert.ToInt32</c> per hour, including its banker's rounding for a non-integral value -
        /// so a model that relied on <c>ProfileOpeningProperties.Profile</c> keeps writing the identical
        /// TBD schedule it wrote before <see cref="DailyAvailabilitySchedule"/> existed. It is NOT routed
        /// through a binary schedule, which could not represent a general-valued curve without altering it.
        /// </para>
        /// </summary>
        public static int[] ScheduleValues(this Profile profile)
        {
            //GetDailyValues() is GetValues(new Range<int>(0, 23)) and therefore returns exactly 24 values
            //whenever the profile carries any values at all; it returns null for a profile carrying none.
            //Both outcomes are handled here rather than by a caller-side length guard, because a guard that
            //skipped the write AFTER the TBD schedule had been created is what left a named, all-zero
            //schedule behind in a real model.
            double[] values = profile?.GetDailyValues();
            if (values == null || values.Length != ScheduleHourCount)
            {
                return null;
            }

            int[] result = new int[ScheduleHourCount];
            for (int hour = 0; hour < ScheduleHourCount; hour++)
            {
                result[hour] = global::System.Convert.ToInt32(values[hour]);
            }

            return result;
        }

        /// <summary>
        /// Whether two sets of TBD schedule values are the same schedule behaviourally - the same
        /// <see cref="ScheduleHourCount"/> values in the same order. Names take no part.
        /// </summary>
        public static bool ScheduleValuesEqual(IEnumerable<int> values_1, IEnumerable<int> values_2)
        {
            if (values_1 == null || values_2 == null)
            {
                return false;
            }

            int[] array_1 = values_1.ToArray();
            int[] array_2 = values_2.ToArray();

            if (array_1.Length != ScheduleHourCount || array_2.Length != ScheduleHourCount)
            {
                return false;
            }

            for (int hour = 0; hour < ScheduleHourCount; hour++)
            {
                if (array_1[hour] != array_2[hour])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
