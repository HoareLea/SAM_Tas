// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The air change rate that REALISES a space's whole supply-air requirement</b> - what
        /// <c>ticV.factor</c> must be when, and only when, a SAM Ventilation profile has chosen TBD Building
        /// Simulator mechanical ventilation as the way that requirement is delivered.
        /// <para>
        /// <see cref="Analytical.Query.CalculatedSupplyAirFlow(Space)"/> SUMS the four bases SAM lets an
        /// engineer state a requirement on - <c>SupplyAirFlow</c>, <c>SupplyAirFlowPerArea</c>,
        /// <c>SupplyAirFlowPerPerson</c> and <c>SupplyAirChangesPerHour</c>. <b>Every</b> one of them is
        /// included here. A profile assigned to this internal condition is a deliberate statement that the
        /// Building Simulator delivers the required air, so it must deliver ALL of it; leaving a basis out
        /// would under-ventilate the zone in the simulation by exactly that term.
        /// </para>
        /// <para>
        /// <b>The per-person basis is included even though <c>InternalGain.freshAirRate</c> also holds it.</b>
        /// That is TAS's Outside Air field: it feeds Part L / EPC reporting and is available to Tas Systems,
        /// but it does not itself supply air to the thermal zone in the TSD simulation. Holding the same rate
        /// in both is therefore not a Building Simulator double count - the two fields answer different
        /// questions. (An earlier revision of this fix subtracted the per-person term here on the opposite
        /// assumption. It made the round trip stable, but by changing the physical total.)
        /// </para>
        /// <para>
        /// <b>What stops it compounding is elsewhere.</b> This total used to be written into <c>ticV.factor</c>
        /// and read back as the <c>SupplyAirChangesPerHour</c> BASIS, so the next export summed the other bases
        /// on top of a figure that already contained them and the rate grew once per generation, without bound
        /// (a licensed bedroom went 1.72 -> 2.44 -> 3.16 ACH). The feedback is broken by
        /// <see cref="SAMZoneMetadata"/>, which carries the authored decomposition across the seam so the
        /// import restores the bases instead of inferring one from the total - not by changing what the total
        /// is.
        /// </para>
        /// </summary>
        /// <param name="space">The space, which supplies the occupancy the per-person basis needs and the volume.</param>
        /// <returns>The ACH to write, or <c>double.NaN</c> when the space states no supply air at all, or no usable volume.</returns>
        public static double VentilationAirChangesPerHour(this Space space)
        {
            if (space == null)
            {
                return double.NaN;
            }

            double airFlow = Analytical.Query.CalculatedSupplyAirFlow(space);
            if (double.IsNaN(airFlow))
            {
                return double.NaN;
            }

            if (!space.TryGetValue(Analytical.SpaceParameter.Volume, out double volume) || double.IsNaN(volume) || volume <= 0)
            {
                //No volume to convert through. The caller's own fallback decides what to do; saying NaN here
                //is honest, and matches what CalculatedSupplyAirFlow says when it can state nothing.
                return double.NaN;
            }

            return airFlow / volume * 3600.0;
        }
    }
}
