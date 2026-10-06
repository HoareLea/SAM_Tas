// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        /// <summary>
        /// The number of hours a TAS yearly plant schedule describes.
        /// </summary>
        public const int HoursPerYear = 8760;

        /// <summary>
        /// Whether the operation carrier TAS holds for a fan states the frozen parity configuration -
        /// continuous operation, factor <c>1.0</c> - and if not, why not.
        /// <para>
        /// <b>How factor 1.0 is represented natively.</b> A TAS plant schedule is an on/off table: the
        /// SAM <c>YearlySchedule</c> PR1 carries is written by <c>Modify.Add(EnergyCentre, ISchedule)</c>
        /// as a <c>tpdScheduleYearly</c> through <c>SetYearlyValues(int[8760])</c>, and a fan runs in the
        /// hours that table is on. So constant operation at factor 1.0 is a yearly schedule on in every
        /// one of its 8760 hours, which TAS reports as <c>GetNumOperableHours() == 8760</c>. Nothing
        /// weaker is accepted.
        /// </para>
        /// <para>
        /// <b>A function schedule is refused, even where it happened to run continuously.</b>
        /// <c>tpdScheduleFunction</c> - the shipped <c>MV.json</c> fans' <c>"Occupancy Schedule"</c>,
        /// <c>tpdScheduleFunctionAllZonesLoad</c> on occupant-sensible load - switches the fan with the
        /// attached zones' demand. Measured on licensed TAS, reading each fan's hourly Load: on the
        /// acceptance fixture it happened to deliver the full duty in all 8760 hours, because some
        /// attached room carried occupant load in every hour; the same document with only that
        /// function's load bits changed to heating ran every fan in <b>0</b> of 8760 hours, because no
        /// free-running zone ever demanded heat. Continuity it owes to one dwelling's occupancy is not the
        /// frozen factor 1.0. <c>GetNumOperableHours()</c> throws <c>"Not a Yearly Schedule"</c> on it.
        /// A yearly table off for 24 hours answers <c>8736</c>, and the fans ran in exactly those 8736.
        /// </para>
        /// <para>
        /// <b>No schedule at all is refused</b>, because then nothing in the document states the
        /// operation the parity configuration freezes; and an <b>hourly</b> (day-type) schedule is
        /// refused because TAS reports operable hours only for a yearly table, so its continuity cannot
        /// be read back.
        /// </para>
        /// </summary>
        /// <param name="scheduleType">The native <c>tpdScheduleType</c> of the fan's schedule, or <c>null</c> when none is attached.</param>
        /// <param name="operableHours">What TAS reports from <c>GetNumOperableHours()</c> for a yearly schedule, or <c>null</c> when it was not read.</param>
        /// <returns><c>null</c> when the carrier states continuous operation at factor 1.0; otherwise the reason it does not.</returns>
        public static string ContinuousOperationRefusal(int? scheduleType, int? operableHours)
        {
            if (scheduleType == null || scheduleType.Value == (int)tpdScheduleType.tpdScheduleNone)
            {
                return "carries no operating schedule, so nothing in the document states the continuous operation "
                    + "(factor 1.0) the parity configuration requires. Supply the constant 1.0 yearly schedule "
                    + "through the materialisation's settings.";
            }

            switch ((tpdScheduleType)scheduleType.Value)
            {
                case tpdScheduleType.tpdScheduleYearly:
                    if (operableHours == HoursPerYear)
                    {
                        return null;
                    }

                    return string.Format(
                        "carries a yearly schedule operable in {0} of {1} hours, so it would switch the fan off "
                        + "in the hours it does not cover; the parity configuration requires continuous "
                        + "operation (factor 1.0) in every hour.",
                        operableHours.HasValue ? operableHours.Value.ToString(global::System.Globalization.CultureInfo.InvariantCulture) : "an unreadable number",
                        HoursPerYear);

                case tpdScheduleType.tpdScheduleFunction:
                    return "carries a function schedule, which switches the fan with the attached zones' demand "
                        + "rather than running it in every hour; the parity configuration requires continuous "
                        + "operation (factor 1.0). Supply the constant 1.0 yearly schedule through the "
                        + "materialisation's settings.";

                case tpdScheduleType.tpdScheduleHourly:
                    return "carries an hourly (day-type) schedule, whose operable hours TAS does not report, so "
                        + "continuous operation (factor 1.0) in every hour cannot be read back. Supply the "
                        + "constant 1.0 yearly schedule through the materialisation's settings.";

                default:
                    return string.Format(
                        "carries a schedule of unrecognised type {0}, so its operation cannot be settled.",
                        scheduleType.Value);
            }
        }
    }
}
