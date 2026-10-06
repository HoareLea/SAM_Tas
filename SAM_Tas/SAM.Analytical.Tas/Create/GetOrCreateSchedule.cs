// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        /// <summary>
        /// The single seam from a SAM availability schedule to a <c>TBD.schedule</c>: reuse an existing
        /// building schedule with the same values, or create exactly one new schedule under a deterministic
        /// name.
        /// </summary>
        public static TBD.schedule GetOrCreateSchedule(this TBD.Building building, DailyAvailabilitySchedule dailyAvailabilitySchedule, out string refusal)
        {
            return GetOrCreateSchedule(building, dailyAvailabilitySchedule, out refusal, null);
        }

        /// <summary>
        /// The same seam, answering the "does the building already have these values?" question from a
        /// <see cref="BuildingReuseCache"/> instead of re-reading every schedule. Semantics are identical -
        /// see the <c>cache</c> remarks on the name/values overload.
        /// </summary>
        public static TBD.schedule GetOrCreateSchedule(this TBD.Building building, DailyAvailabilitySchedule dailyAvailabilitySchedule, out string refusal, BuildingReuseCache cache)
        {
            refusal = null;

            if (dailyAvailabilitySchedule == null)
            {
                return null;
            }

            return GetOrCreateSchedule(building, dailyAvailabilitySchedule.Name, dailyAvailabilitySchedule.ScheduleValues(), out refusal, cache);
        }

        /// <summary>
        /// The single seam from a requested name plus <c>int[24]</c> to a <c>TBD.schedule</c>.
        /// <para>
        /// <b>Reuse is by VALUE, creation is by name.</b> A TBD schedule belongs to the building and is
        /// shared by apertures in every zone, so identity here is the 24 values, never the name. Openings
        /// with quite different reasons for their availability window - a Part O window, an internal door,
        /// a security restriction - share one schedule when their values match, whatever each calls it.
        /// This is what makes repeated export idempotent instead of accumulating
        /// <c>PartO_DayOpen_08_23 (1)</c>, <c>(2)</c>, ...
        /// </para>
        /// <para>
        /// <b>Nothing is created until the source is known to be valid.</b> The order below is deliberate:
        /// validate, search, and only then create. <c>TBD.Building</c> exposes <c>RemoveApertureType</c>,
        /// <c>RemoveConstruction</c>, <c>RemoveIC</c>, <c>RemoveSubstituteElement</c>, <c>RemoveIZAM</c> and
        /// <c>RemoveZoneGroup</c> - but <b>no</b> <c>RemoveSchedule</c>, so a schedule created against an
        /// invalid source could never be withdrawn. The previous code created and named the schedule first
        /// and validated the source afterwards, which is how a named schedule holding 24 zeros could be left
        /// behind with nothing reporting it.
        /// </para>
        /// <list type="number">
        /// <item>validate exactly <see cref="Query.ScheduleHourCount"/> source values - refuse, creating nothing, if not;</item>
        /// <item>read every existing building schedule's 24 values;</item>
        /// <item>reuse the first value-identical schedule, whatever its name;</item>
        /// <item>otherwise derive a deterministic, collision-safe name (never overwriting a different-valued schedule);</item>
        /// <item>create one schedule, reserve its name against later failure, write all 24 values, and read all 24 back;</item>
        /// <item>refuse if the read-back disagrees - the caller must not assign a schedule that did not persist.</item>
        /// </list>
        /// </summary>
        /// <param name="refusal">Why no schedule could be produced, or null on success.</param>
        public static TBD.schedule GetOrCreateSchedule(this TBD.Building building, string name, IEnumerable<int> values, out string refusal)
        {
            return GetOrCreateSchedule(building, name, values, out refusal, null);
        }

        /// <summary>
        /// The same seam, taking a <see cref="BuildingReuseCache"/>.
        /// <para>
        /// <b>Behaviour is identical in every respect</b> - validate, reuse by value, name deterministically,
        /// create once, verify by reading all 24 back, refuse rather than overwrite. The cache changes only
        /// WHERE the list of existing schedules comes from: read once per open document instead of rebuilt
        /// per opening child, which on a model with hundreds of scheduled windows is the difference between
        /// one pass over the building's schedules and one pass per child. A schedule this call creates is
        /// registered back, so the next child reuses it exactly as it would have found it by re-reading.
        /// </para>
        /// <para>
        /// <paramref name="cache"/> may be null, in which case the building is read directly and this is the
        /// original overload byte for byte.
        /// </para>
        /// </summary>
        public static TBD.schedule GetOrCreateSchedule(this TBD.Building building, string name, IEnumerable<int> values, out string refusal, BuildingReuseCache cache)
        {
            refusal = null;

            if (building == null)
            {
                refusal = "No TBD building to create or reuse a schedule in.";
                return null;
            }

            //1. Validate the source BEFORE anything is created.
            int[] requested = values?.ToArray();
            if (requested == null || requested.Length != Query.ScheduleHourCount)
            {
                refusal = string.Format("A TBD schedule needs exactly {0} hourly values; {1} were supplied for '{2}'. Nothing was created.", Query.ScheduleHourCount, requested == null ? "none" : requested.Length.ToString(), name ?? "(unnamed)");
                return null;
            }

            //2/3. Reuse by value, whatever the existing schedule is called.
            List<KeyValuePair<TBD.schedule, int[]>> schedules = cache == null ? building.SchedulesWithValues() : cache.SchedulesWithValues();

            int index = Query.ScheduleIndex(schedules.Select(x => x.Value), requested);
            if (index != -1)
            {
                return schedules[index].Key;
            }

            //4. A new schedule is genuinely needed. Any existing schedule sharing the requested name either
            //   has different values or could not be read (or step 3 would have matched it), so in neither
            //   case may it be overwritten. The cache's name list also carries RESERVED names - schedules
            //   this session created and named but whose write then failed: they can never be reused, but
            //   their names still occupy the namespace.
            string name_Schedule = Query.ScheduleName(cache == null ? schedules.Select(x => x.Key?.name) : (IEnumerable<string>)cache.ScheduleNames(), name, requested, out refusal);
            if (name_Schedule == null)
            {
                return null;
            }

            //5. Create exactly one schedule and write it.
            TBD.schedule schedule = building.AddSchedule();
            if (schedule == null)
            {
                refusal = string.Format("TBD refused to add a schedule for '{0}'.", name_Schedule);
                return null;
            }

            schedule.name = name_Schedule;

            //The name is reserved the moment it exists in the TBD. If the write below fails, the schedule
            //stays behind (there is no RemoveSchedule) and is never registered as reusable - but its name
            //must still not be chosen again by the next creation.
            cache?.ReserveScheduleName(name_Schedule);

            //6. Write and verify. A failed verification returns null, so the caller never assigns it.
            if (!schedule.SetScheduleValues(requested, out refusal))
            {
                return null;
            }

            //7. Only a verified schedule is registered, so a failed write can never be handed to the next
            //   caller as a reusable one.
            cache?.RegisterSchedule(schedule, requested);

            return schedule;
        }
    }
}
