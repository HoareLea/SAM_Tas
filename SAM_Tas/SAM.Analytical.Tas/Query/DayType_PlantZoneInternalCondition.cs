// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Whether the named calendar daytype gets an internal condition on an air handling unit's
        /// generated plant zone.
        ///
        /// <para><b>HDD and CDD deliberately do not</b></para>
        /// <para>
        /// <c>Modify.UpdateIZAMs</c> builds one small TAS zone per air handling unit - the unit's own plant
        /// zone, named after the unit ("MVHR-01" and so on) - and assigns it an internal condition on the
        /// daytypes this returns true for. The two design daytypes, <b>HDD</b> (heating design day) and
        /// <b>CDD</b> (cooling design day), are excluded on purpose: the generated zone is a duct volume
        /// standing in for a unit, not a room, and it is not wanted active in the design-day sizing runs.
        /// </para>
        /// <para>
        /// TAS's pre-simulation check notices and says so - <i>"Zone 'MVHR-01' is missing internal
        /// conditions on some daytypes"</i> - and that message is <b>expected</b>. It is a warning about an
        /// intended state, not a defect: adding HDD and CDD internal conditions to silence it would put the
        /// plant zone into the design-day runs, which is the thing being avoided. Nothing on the Part O
        /// pre-simulation path promotes it to an error either.
        /// </para>
        /// <para>
        /// Named and separated from <c>UpdateIZAMs</c> so that the exclusion is a decision with a name that
        /// can be read and pinned, rather than a predicate inside a COM loop no test can reach.
        /// </para>
        /// </summary>
        /// <param name="dayTypeName">The daytype's name, as the TBD calendar states it.</param>
        /// <returns>False for "HDD" and "CDD"; true for every other daytype, including an unnamed one.</returns>
        public static bool DayType_PlantZoneInternalCondition(string dayTypeName)
        {
            //An unnamed daytype is kept. It is not one of the two the exclusion is about, and dropping
            //every daytype a calendar failed to name would silently shrink the schedule the plant zone
            //runs on.
            if (string.IsNullOrWhiteSpace(dayTypeName))
            {
                return true;
            }

            return !dayTypeName.Equals("HDD") && !dayTypeName.Equals("CDD");
        }
    }
}
