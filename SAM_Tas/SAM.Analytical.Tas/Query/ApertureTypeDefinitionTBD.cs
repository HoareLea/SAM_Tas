// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The day types a TBD aperture type applies on, by name.
        /// <para>
        /// <b>S1-C0 probe result (licensed TAS, 2026-08-21).</b> <c>TBD.IApertureType.GetDayType(int)</c>
        /// exists in the Interop.TBD metadata and reads back exactly what <c>SetDayType</c> wrote: a strict
        /// subset comes back as that subset, an aperture type with none comes back empty, a removed day type
        /// disappears, a duplicated write does not duplicate the entry, and membership survives save and
        /// reopen. The collection is 0-based and null-terminated, and the order is the order
        /// <c>SetDayType</c> was called in - NOT calendar order - which is why membership is compared as a
        /// set everywhere it is compared.
        /// </para>
        /// </summary>
        /// <param name="refusal">Why membership could not be read, or null on success.</param>
        public static List<string> DayTypeNames(this TBD.ApertureType apertureType, out string refusal)
        {
            refusal = null;

            if (apertureType == null)
            {
                refusal = "No TBD aperture type to read day-type membership from.";
                return null;
            }

            List<string> result = new List<string>();

            try
            {
                int index = 0;
                TBD.dayType dayType = apertureType.GetDayType(index);
                while (dayType != null)
                {
                    result.Add(dayType.name);
                    index++;
                    dayType = apertureType.GetDayType(index);
                }
            }
            catch (global::System.Exception exception)
            {
                refusal = string.Format("The day types of TBD aperture type '{0}' could not be read ({1}).", apertureType.name, exception.Message);
                return null;
            }

            return result;
        }

        /// <summary>
        /// An EXISTING TBD aperture type read back as the definition it represents, or null when this
        /// export must not reuse it.
        /// <para>
        /// <b>Refusing to read is the safe direction.</b> A definition is reused by being assigned to
        /// further building elements, so misreading one would apply the wrong opening control to every
        /// element that adopts it. Anything this export cannot fully account for is therefore reported as
        /// non-reusable with a reason: its name still occupies the namespace so a new type cannot collide
        /// with it, and it takes no other part.
        /// </para>
        /// <para>The gates, and what each protects against:</para>
        /// <list type="bullet">
        /// <item><c>sheltered</c> other than the authored default - a shelter this export never states would silently arrive with the type.</item>
        /// <item>no readable profile - there is nothing to compare.</item>
        /// <item><c>value</c> other than 1 - every control this export writes sets it to 1, so anything else was authored elsewhere. This is also what excludes a bare, never-written aperture type.</item>
        /// <item>a profile type other than <c>ticValueProfile</c>/<c>ticFunctionProfile</c> - <c>ticHourlyFunctionProfile</c> and friends are not shapes this export writes or can compare.</item>
        /// <item><c>ticFunctionProfile</c> with no function text - not a shape this export writes.</item>
        /// <item>a schedule whose 24 values will not read back, or which sits alongside a non-zero <c>setbackValue</c> - the setback is what an OFF hour selects, so an availability schedule is only meaningful with a setback of 0.</item>
        /// <item>unreadable day-type membership.</item>
        /// </list>
        /// </summary>
        /// <param name="refusal">Why the aperture type may not be reused, or null when it may.</param>
        public static ApertureTypeDefinition ApertureTypeDefinition(this TBD.ApertureType apertureType, out string refusal)
        {
            refusal = null;

            if (apertureType == null)
            {
                refusal = "No TBD aperture type to read.";
                return null;
            }

            string name = apertureType.name;

            try
            {
                if (apertureType.sheltered != 0)
                {
                    refusal = string.Format("TBD aperture type '{0}' is sheltered, which this export never writes, so it was not reused.", name);
                    return null;
                }

                //dischargeCoefficient and GetProfile() sit on the concrete COM type, not the marshalled
                //interface - the same dynamic access the write and the SAM read both use.
                dynamic @dynamic = apertureType;

                TBD.profile profile = @dynamic.GetProfile();
                if (profile == null)
                {
                    refusal = string.Format("TBD aperture type '{0}' has no readable profile, so its opening control could not be compared and it was not reused.", name);
                    return null;
                }

                float value = global::System.Convert.ToSingle(profile.value);
                if (value != 1)
                {
                    refusal = string.Format("TBD aperture type '{0}' carries a profile value of {1}; every control this export writes carries 1, so it was not reused.", name, value);
                    return null;
                }

                if (profile.type != TBD.ProfileTypes.ticValueProfile && profile.type != TBD.ProfileTypes.ticFunctionProfile)
                {
                    refusal = string.Format("TBD aperture type '{0}' carries a {1} profile, which is not a shape this export writes, so it was not reused.", name, profile.type);
                    return null;
                }

                string function = profile.function;
                bool functionProfile = profile.type == TBD.ProfileTypes.ticFunctionProfile;
                if (functionProfile && string.IsNullOrWhiteSpace(function))
                {
                    refusal = string.Format("TBD aperture type '{0}' carries a function profile with no function text, which is not a shape this export writes, so it was not reused.", name);
                    return null;
                }

                int[] values_Schedule = null;
                TBD.schedule schedule = profile.schedule;
                if (schedule != null)
                {
                    values_Schedule = schedule.HourlyValues();
                    if (values_Schedule == null)
                    {
                        refusal = string.Format("The schedule on TBD aperture type '{0}' did not read back {1} hourly values, so its opening control could not be compared and it was not reused.", name, ScheduleHourCount);
                        return null;
                    }

                    float setbackValue = global::System.Convert.ToSingle(profile.setbackValue);
                    if (setbackValue != 0)
                    {
                        refusal = string.Format("TBD aperture type '{0}' carries a schedule alongside a setback value of {1}. An OFF hour selects the setback, so an availability schedule is only meaningful with a setback of 0; the type was not reused.", name, setbackValue);
                        return null;
                    }
                }

                List<string> dayTypeNames = DayTypeNames(apertureType, out refusal);
                if (dayTypeNames == null)
                {
                    return null;
                }

                ApertureTypeProfileMode mode = functionProfile
                    ? ApertureTypeProfileMode.Function
                    : (values_Schedule != null ? ApertureTypeProfileMode.ScheduleOnly : ApertureTypeProfileMode.Plain);

                return new ApertureTypeDefinition(
                    global::System.Convert.ToSingle(@dynamic.dischargeCoefficient),
                    global::System.Convert.ToSingle(profile.factor),
                    mode,
                    function,
                    values_Schedule,
                    apertureType.description,
                    dayTypeNames);
            }
            catch (global::System.Exception exception)
            {
                refusal = string.Format("TBD aperture type '{0}' could not be read ({1}), so it was not reused.", name, exception.Message);
                return null;
            }
        }
    }
}
