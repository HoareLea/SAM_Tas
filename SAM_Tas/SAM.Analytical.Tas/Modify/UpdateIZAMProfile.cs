// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        /// <summary>
        /// Writes one SAM air movement onto a TBD inter-zone air movement's profile, <b>converting the
        /// volumetric flow SAM states into the mass flow TAS reads</b>.
        ///
        /// <para><b>This is the unit boundary, and it is the only one.</b></para>
        /// <para>
        /// A TBD IZAM profile is a mass flow rate in kg/s - the EDSL Building Simulator documentation states
        /// the Inter-Zone Air Movement flow rate that way, and a licensed TBD reports the profile's own
        /// <c>units</c> as <c>kg/s</c>. SAM states the same air volumetrically, in m3/s, all the way from the
        /// Approved Document F terminal duty to <c>SpaceAirMovement.AirFlow</c>. Neither type says which it
        /// is, so passing the SAM number straight through compiles, balances, simulates and is wrong by the
        /// density of air.
        /// </para>
        /// <para>
        /// Every inter-zone air movement <c>Modify.UpdateIZAMs</c> writes goes through here, whatever its
        /// shape - <c>Outside -> unit</c>, <c>unit -> space</c>, <c>space -> space</c> transfer,
        /// <c>space -> unit</c> extract, <c>unit -> Outside</c> exhaust - so the whole network is converted
        /// at one density and a network that balanced by volume balances by mass exactly.
        /// </para>
        /// </summary>
        /// <param name="profile_TBD">The IZAM's profile, from <c>TBD.IZAM.GetProfile()</c>.</param>
        /// <param name="profile">
        /// The movement's own profile - when it runs, and at what fraction of its design flow.
        /// </param>
        /// <param name="airFlow_M3PerSecond">
        /// The movement's design flow as SAM states it, <b>volumetric, in m3/s</b>. Converted here; do not
        /// convert before calling.
        /// </param>
        /// <returns>False where nothing was written.</returns>
        public static bool UpdateIZAMProfile(this TBD.profile profile_TBD, Profile profile, double airFlow_M3PerSecond)
        {
            return Update(profile_TBD, profile, Query.IZAMMassFlow_KgPerSecond(airFlow_M3PerSecond));
        }
    }
}
