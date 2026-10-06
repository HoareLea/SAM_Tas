// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The one conversion from a raw <c>TBD.ISchedule</c> value to the SAM representation.</b>
        /// <para>
        /// <c>TBD.ISchedule::get_values</c> is typed <c>int</c>, but TAS holds a schedule hour as a boolean
        /// and hands TRUE back as the <c>VARIANT_BOOL</c> bit pattern, which widens to <b>-1</b>. An hour
        /// written as <c>1</c> therefore reads back as <c>-1</c> from the very same COM object. That is the
        /// only difference between the two representations, so -1 maps to 1 and <b>every other value is
        /// passed through unchanged</b>: an unexpected read-back stays unexpected and still fails the
        /// caller's comparison, rather than being quietly taken for "true".
        /// </para>
        /// <para>
        /// Passing other values through also keeps the legacy general-valued
        /// <see cref="Query.ScheduleValues(Profile)"/> route readable. Only <see cref="DailyAvailabilitySchedule"/>
        /// below decides what counts as binary.
        /// </para>
        /// </summary>
        public static int ScheduleValueFromTBD(int value)
        {
            return value == -1 ? 1 : value;
        }

        /// <summary>
        /// <b>The one conversion from a SAM hour to the TBD slot that holds it.</b>
        /// <para>
        /// A SAM hour <c>h</c> is the interval <c>h:00 -&gt; (h+1):00</c> and lives at array index <c>h</c>,
        /// so index 0 is 00:00-01:00 and index 23 is 23:00-24:00. <b>TBD's 24-slot hourly indexed
        /// properties are 1-based hour-ending</b>: slot <c>n</c> is the hour ENDING at <c>n:00</c>, so the
        /// valid slots are 1..24 and slot 9 is 08:00-09:00. SAM hour <c>h</c> is therefore TBD slot
        /// <c>h + 1</c>.
        /// </para>
        /// <para>
        /// <b>The evidence.</b> <c>TBD.ISchedule::values[int]</c> carries no documentation of its own, but
        /// its structural sibling on the same library - <c>TBD.IProfile::hourlyValues[int]</c>, the other
        /// 24-slot hourly indexed property - is addressed 1-based at three long-standing production sites:
        /// <c>Query.DailyValues</c> (<c>for i = 1..24: result[i - 1] = profile.hourlyValues[i]</c>),
        /// <c>Modify.Update</c> (<c>profile_TBD.hourlyValues[i + 1] = profile[i]</c>) and
        /// <c>SAM.Core.Tas.Query.Values</c> (<c>result.Add(profile.hourlyValues[i + 1])</c>). Writing the
        /// schedule 0-based against that convention put every hour one slot early, which is exactly what a
        /// licensed TAS showed for <c>PartO_DayOpen_08_23</c>: a 15-hour ON block starting at 07:00-08:00
        /// and ending at 21:00-22:00 instead of 08:00-09:00 to 22:00-23:00. Note that TBD's COLLECTION
        /// getters (<c>GetSchedule</c>, <c>GetBuildingElement</c>, ...) are 0-based - it is the hourly
        /// indexed properties, and only those, that start at 1.
        /// </para>
        /// <para>
        /// Kept separate from <see cref="ScheduleValueFromTBD(int)"/> on purpose: the index and the value
        /// are two independent COM conventions and a reader has to be able to see each of them.
        /// </para>
        /// </summary>
        public static int ScheduleIndexTBD(int hour)
        {
            return hour + 1;
        }

        /// <summary>
        /// The SAM hour held in a TBD slot - the inverse of <see cref="ScheduleIndexTBD(int)"/>, so a
        /// read-back restores SAM's canonical 0..23 hour-beginning representation.
        /// </summary>
        public static int ScheduleHourFromTBD(int index_TBD)
        {
            return index_TBD - 1;
        }

        /// <summary>
        /// All <see cref="ScheduleHourCount"/> hourly values of a TBD schedule in the SAM representation, or
        /// null when it cannot be read. <c>TBD.ISchedule</c> has no count member, so the 24 slots are read
        /// explicitly: <see cref="ScheduleIndexTBD(int)"/> picks the slot, then
        /// <see cref="ScheduleValueFromTBD(int)"/> normalises the value it holds.
        /// </summary>
        public static int[] HourlyValues(this TBD.schedule schedule)
        {
            if (schedule == null)
            {
                return null;
            }

            int[] result = new int[ScheduleHourCount];
            try
            {
                for (int hour = 0; hour < ScheduleHourCount; hour++)
                {
                    int index_TBD = ScheduleIndexTBD(hour);
                    result[hour] = ScheduleValueFromTBD(schedule.get_values(index_TBD));
                }
            }
            catch
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// A TBD schedule read back as a SAM <see cref="DailyAvailabilitySchedule"/>, or null when its
        /// values are not binary.
        /// <para>
        /// A general-valued schedule - which only a user-authored TAS model can produce - is deliberately
        /// NOT coerced into a binary one. The TBD -&gt; SAM conversion keeps it as the legacy general-valued
        /// <see cref="ProfileOpeningProperties.Profile"/> instead, so a round trip does not change what the
        /// user authored.
        /// </para>
        /// </summary>
        public static DailyAvailabilitySchedule DailyAvailabilitySchedule(this TBD.schedule schedule)
        {
            int[] values = HourlyValues(schedule);
            if (values == null)
            {
                return null;
            }

            bool[] binaryValues = new bool[ScheduleHourCount];
            for (int hour = 0; hour < ScheduleHourCount; hour++)
            {
                if (values[hour] != 0 && values[hour] != 1)
                {
                    return null;
                }

                binaryValues[hour] = values[hour] == 1;
            }

            string name = string.IsNullOrWhiteSpace(schedule.name) ? null : schedule.name;
            return new DailyAvailabilitySchedule(name, binaryValues);
        }

        /// <summary>
        /// Every TBD schedule in the building paired with its <see cref="ScheduleHourCount"/> values, in
        /// building order - the input to value-based reuse.
        /// </summary>
        public static List<KeyValuePair<TBD.schedule, int[]>> SchedulesWithValues(this TBD.Building building)
        {
            List<KeyValuePair<TBD.schedule, int[]>> result = new List<KeyValuePair<TBD.schedule, int[]>>();

            List<TBD.schedule> schedules = building?.Schedules();
            if (schedules == null)
            {
                return result;
            }

            foreach (TBD.schedule schedule in schedules)
            {
                result.Add(new KeyValuePair<TBD.schedule, int[]>(schedule, HourlyValues(schedule)));
            }

            return result;
        }
    }
}
