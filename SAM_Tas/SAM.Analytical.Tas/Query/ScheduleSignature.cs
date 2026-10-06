// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// A deterministic, build-stable fingerprint of a set of TBD schedule values, used to generate and
        /// to collision-resolve TBD schedule NAMES.
        /// <para>
        /// For a binary schedule - which is what everything SAM writes is - this is
        /// <see cref="DailyAvailabilitySchedule.Signature"/>: six uppercase hexadecimal digits of the
        /// 24-bit mask, hour 0 as the most significant bit. The default Part O window 08:00-23:00 is
        /// <c>00FFFE</c>. Deferring to the domain object keeps one definition of that mask rather than two.
        /// </para>
        /// <para>
        /// A non-binary schedule can only arrive through the legacy <see cref="Profile"/> route (a
        /// user-authored general-valued curve). Those get an <c>X</c>-prefixed 8-digit FNV-1a hash of the
        /// values instead, so a non-binary signature can never be mistaken for, or collide with, a binary
        /// one. The hash is computed arithmetically here rather than through <c>GetHashCode</c>, which is
        /// not stable across runtimes or builds and must never appear in a persisted TBD name.
        /// </para>
        /// </summary>
        public static string ScheduleSignature(IEnumerable<int> values)
        {
            if (values == null)
            {
                return null;
            }

            int[] array = values.ToArray();
            if (array.Length != ScheduleHourCount)
            {
                return null;
            }

            if (array.All(x => x == 0 || x == 1))
            {
                bool[] binaryValues = new bool[ScheduleHourCount];
                for (int hour = 0; hour < ScheduleHourCount; hour++)
                {
                    binaryValues[hour] = array[hour] == 1;
                }

                return new DailyAvailabilitySchedule(null, binaryValues).Signature;
            }

            //FNV-1a over the four bytes of each value, little-endian, hour 0 first.
            uint hash = 2166136261;
            for (int hour = 0; hour < ScheduleHourCount; hour++)
            {
                uint value = unchecked((uint)array[hour]);
                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash = unchecked((hash ^ ((value >> shift) & 0xFF)) * 16777619);
                }
            }

            return string.Format("X{0}", hash.ToString("X8"));
        }
    }
}
