// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// <b>The one place in this assembly that writes <c>TBD.schedule</c> values</b>, and the one place
        /// that verifies the write actually took.
        /// <para>
        /// <b>On the two spellings of the write.</b> <c>schedule.values[i] = x</c> and
        /// <c>schedule.set_values(i, x)</c> are the same operation, not a correct one and a defective one:
        /// C# supports indexed properties only for COM interop types and lowers both to
        /// <c>TBD.ISchedule::set_values(int32, int32)</c>. Verified by disassembling this assembly's own IL,
        /// where the two former call sites emitted the identical instruction. Neither spelling was the cause
        /// of a schedule that reached a real TBD holding 24 zeros.
        /// </para>
        /// <para>
        /// <b>Why the read-back is not optional.</b> A TBD schedule that persists its name while losing its
        /// values is indistinguishable, downstream, from a schedule that was meant to be all-zero - and
        /// <c>TBD.Building</c> exposes no <c>RemoveSchedule</c>, so a bad schedule cannot be cleaned up
        /// afterwards. Reading all written indices straight back is 24 cheap COM calls and converts that
        /// class of silent corruption into a reported refusal.
        /// </para>
        /// <para>
        /// <b>Two independent COM conventions are crossed here, and they are crossed separately.</b>
        /// <see cref="Query.ScheduleIndexTBD(int)"/> maps the SAM hour to the TBD slot that holds it - TBD's
        /// hourly indexed properties are 1-based hour-ending, so SAM hour <c>h</c> is slot <c>h + 1</c> and
        /// the slots written are 1..24, never 0. <see cref="Query.ScheduleValueFromTBD(int)"/> then maps the
        /// value TAS hands back - an ON hour comes back as the <c>VARIANT_BOOL</c> TRUE bit pattern, -1, not
        /// as the 1 that was written. Writing and reading at the same wrong index masked the first of these
        /// completely: the read-back agreed with the write while a licensed TAS showed every hour one cell
        /// early.
        /// </para>
        /// <para>
        /// The value normalisation is not a relaxation: only -1 is folded onto 1, so any other unexpected
        /// read-back still refuses.
        /// </para>
        /// </summary>
        /// <param name="values">
        /// SAM hours 0..n-1, written to TBD slots 1..n. Callers writing an availability schedule pass
        /// exactly <see cref="Query.ScheduleHourCount"/> values and so fill slots 1..24; the legacy
        /// <c>Create.Schedule</c> route may pass fewer, leaving the later slots untouched, and then only the
        /// slots it wrote are verified.
        /// </param>
        /// <param name="refusal">Why the write is not trustworthy, or null on success.</param>
        public static bool SetScheduleValues(this TBD.schedule schedule, IEnumerable<int> values, out string refusal)
        {
            refusal = null;

            if (schedule == null)
            {
                refusal = "No TBD schedule to write to.";
                return false;
            }

            if (values == null)
            {
                refusal = "No schedule values to write.";
                return false;
            }

            int[] array = values.ToArray();
            if (array.Length == 0)
            {
                refusal = "No schedule values to write.";
                return false;
            }

            if (array.Length > Query.ScheduleHourCount)
            {
                refusal = string.Format("A TBD schedule holds at most {0} hourly values; {1} were supplied.", Query.ScheduleHourCount, array.Length);
                return false;
            }

            for (int hour = 0; hour < array.Length; hour++)
            {
                schedule.set_values(Query.ScheduleIndexTBD(hour), array[hour]);
            }

            //Read every written slot back through the same COM object. A mismatch here is the exact failure
            //that previously reached a real TBD unreported. Index first, then value: the slot comes from
            //Query.ScheduleIndexTBD and the value it holds is normalised by Query.ScheduleValueFromTBD -
            //see the remarks. Both the SAM hour and the TBD slot are named in the refusal, along with the
            //raw COM value, so a genuine persistence failure stays diagnosable.
            for (int hour = 0; hour < array.Length; hour++)
            {
                int index_TBD = Query.ScheduleIndexTBD(hour);
                int value_TBD = schedule.get_values(index_TBD);
                int value = Query.ScheduleValueFromTBD(value_TBD);
                if (value != array[hour])
                {
                    refusal = string.Format("TBD schedule '{0}' did not persist its values: SAM hour {1} (TBD slot {2}) was written as {3} but reads back as {4} (raw TBD value {5}). The schedule was left unassigned rather than exporting a control that does not match the model.", schedule.name, hour, index_TBD, array[hour], value, value_TBD);
                    return false;
                }
            }

            return true;
        }
    }
}
