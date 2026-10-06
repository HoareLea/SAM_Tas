// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The reference air density [kg/m3] every inter-zone air movement this library writes is converted
        /// at.
        /// <para>
        /// <b>SAM's own value, not a second one.</b> <see cref="Core.FluidProperty.Air.Density"/> is what
        /// <c>Modify.AddAirMovementObjects</c> already writes as an air handling unit's density profile and
        /// what <c>SAMAnalyticalCreateIZAMBySetPoint</c> already offers as its default, so the mass flow a
        /// TBD carries and the density the rest of SAM states about the same air are the same number.
        /// Minting a competing constant here - 1.204 kg/m3 at 20 °C at sea level, say - would make the two
        /// disagree by half a percent for no reason anybody could later reconstruct.
        /// </para>
        /// <para>
        /// <b>One density for the whole network.</b> Air expands as it warms, so a physically exact
        /// conversion would use each movement's own air temperature - and the movements would then no longer
        /// balance by mass at any node, which is precisely what TAS refuses. A single reference density
        /// converts a balanced volumetric network into a balanced mass network exactly, because every term
        /// of every node's sum is scaled by the same factor.
        /// </para>
        /// </summary>
        public const double IZAMAirDensity_KgPerM3 = Core.FluidProperty.Air.Density;

        /// <summary>
        /// Converts a SAM air movement's volumetric flow [m3/s] to the <b>mass flow [kg/s]</b> a TBD
        /// inter-zone air movement is specified in.
        ///
        /// <para><b>Why this exists</b></para>
        /// <para>
        /// The two domains state the same air in different units, and nothing in either type system says so.
        /// Approved Document F sizes a terminal in l/s; <c>SpaceAirMovement.AirFlow</c> carries m3/s; and the
        /// EDSL Building Simulator documentation states the Inter-Zone Air Movement flow rate as a
        /// time-varying <b>mass flow rate in kg/s</b> - which a licensed TBD confirms, its IZAM profile
        /// reporting <c>units=kg/s</c>. Writing a m3/s number into that field understates every flow by the
        /// density, roughly 21%, and does it silently: the model still balances, still simulates and still
        /// produces a result, just one for a dwelling ventilated a fifth less than the one that was
        /// designed.
        /// </para>
        /// <para>
        /// <b>The conversion belongs here and only here.</b> SAM's design and runtime domain stays
        /// volumetric - no Part F requirement, no design terminal duty and no
        /// <c>SpaceAirMovement.AirFlow</c> is restated in kg/s - and this is the one seam at which the
        /// volumetric quantity becomes the TAS one. Every IZAM shape passes through it:
        /// <c>Outside -> unit</c>, <c>unit -> space</c>, <c>space -> space</c> transfer, <c>space -> unit</c>
        /// extract and the unit's <c>-> Outside</c> exhaust.
        /// </para>
        /// </summary>
        /// <param name="airFlow_M3PerSecond">Volumetric flow [m3/s], as SAM states it.</param>
        /// <returns>
        /// Mass flow [kg/s]. <c>NaN</c> is passed through rather than turned into a number: a flow the model
        /// does not state is not a flow of zero.
        /// </returns>
        public static double IZAMMassFlow_KgPerSecond(double airFlow_M3PerSecond)
        {
            if (double.IsNaN(airFlow_M3PerSecond))
            {
                return double.NaN;
            }

            return airFlow_M3PerSecond * IZAMAirDensity_KgPerM3;
        }

        /// <summary>
        /// Converts a TBD inter-zone air movement's mass flow [kg/s] back to the volumetric flow [m3/s] SAM
        /// states, at the same reference density.
        /// <para>
        /// The inverse of <see cref="IZAMMassFlow_KgPerSecond"/>, so that a readback of a written file can be
        /// compared with the SAM movement and with the Approved Document F terminal duty it came from
        /// without anybody restating the density at the comparison.
        /// </para>
        /// </summary>
        public static double IZAMVolumeFlow_M3PerSecond(double massFlow_KgPerSecond)
        {
            if (double.IsNaN(massFlow_KgPerSecond))
            {
                return double.NaN;
            }

            return massFlow_KgPerSecond / IZAMAirDensity_KgPerM3;
        }
    }
}
