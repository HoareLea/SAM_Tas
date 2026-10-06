// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The availability schedule an opening asks the TBD aperture control to carry, resolved from SAM
        /// opening properties to a name plus <c>int[24]</c>.
        /// <para>
        /// <b>Precedence, and why it is precedence rather than a merge:</b>
        /// </para>
        /// <list type="number">
        /// <item>
        /// <see cref="ProfileOpeningProperties.Schedule"/> - the first-class binary availability schedule -
        /// wins whenever it is present. It is the explicit statement.
        /// </item>
        /// <item>
        /// <see cref="ProfileOpeningProperties.Profile"/> - the legacy general-valued carrier - is used only
        /// when there is no <c>Schedule</c>. That is exactly the state of every model saved before
        /// <see cref="DailyAvailabilitySchedule"/> existed, so those models keep writing the schedule they
        /// always wrote, through the identical <c>Convert.ToInt32</c> conversion.
        /// </item>
        /// <item><see cref="PartOOpeningProperties.Schedule"/> for a Part O opening restriction.</item>
        /// <item>anything else: no schedule, and no refusal - most openings legitimately have none.</item>
        /// </list>
        /// <para>
        /// The two sources are never combined. If an opening carries both, the explicit
        /// <c>Schedule</c> is written and the legacy <c>Profile</c> takes no part - it is not silently
        /// blended, and it is not treated as a conflict either, because a TBD -&gt; SAM read legitimately
        /// produces both from one TAS schedule.
        /// </para>
        /// </summary>
        /// <param name="name">The requested schedule name, or null when there is no schedule to write.</param>
        /// <param name="values">
        /// Exactly <see cref="ScheduleHourCount"/> values, or null when there is no schedule to write or the
        /// source could not supply 24 hourly values.
        /// </param>
        /// <param name="refusal">
        /// Set only when the opening DOES state a schedule but that schedule is unusable - so that an
        /// unusable source is reported instead of quietly writing nothing (or, as before, quietly writing
        /// nothing after a TBD schedule had already been created and named).
        /// </param>
        /// <returns>True when a usable schedule source was found.</returns>
        public static bool TryGetOpeningScheduleSource(this ISingleOpeningProperties singleOpeningProperties, out string name, out int[] values, out string refusal)
        {
            name = null;
            values = null;
            refusal = null;

            if (singleOpeningProperties == null)
            {
                return false;
            }

            DailyAvailabilitySchedule dailyAvailabilitySchedule = null;
            Profile profile = null;

            if (singleOpeningProperties is ProfileOpeningProperties profileOpeningProperties)
            {
                dailyAvailabilitySchedule = profileOpeningProperties.Schedule;
                if (dailyAvailabilitySchedule == null)
                {
                    profile = profileOpeningProperties.Profile;
                }
            }
            else if (singleOpeningProperties is PartOOpeningProperties partOOpeningProperties)
            {
                dailyAvailabilitySchedule = partOOpeningProperties.Schedule;
            }

            if (dailyAvailabilitySchedule != null)
            {
                name = dailyAvailabilitySchedule.Name;
                values = dailyAvailabilitySchedule.ScheduleValues();

                if (values == null)
                {
                    refusal = string.Format("Availability schedule '{0}' did not supply exactly {1} hourly values, so no TBD schedule was created or assigned.", dailyAvailabilitySchedule.Name, ScheduleHourCount);
                    name = null;
                    return false;
                }

                return true;
            }

            if (profile != null)
            {
                name = profile.Name;
                values = profile.ScheduleValues();

                if (values == null)
                {
                    //A Profile carrying no values at all is the one case GetDailyValues() returns null for.
                    //Reported rather than skipped: skipping it silently - after creating and naming the TBD
                    //schedule - is what left a named 24-zero schedule in a real model.
                    refusal = string.Format("Legacy opening profile '{0}' carries no {1} hourly values, so no TBD schedule was created or assigned.", profile.Name, ScheduleHourCount);
                    name = null;
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
