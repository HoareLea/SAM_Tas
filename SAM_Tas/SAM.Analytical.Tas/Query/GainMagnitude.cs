// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The authored MAGNITUDE a TBD internal-gain profile carries - the engineering quantity the
        /// profile's hourly values scale, in whatever unit that slot is written in (W/m2 for a gain,
        /// ACH for infiltration/ventilation, and so on).
        /// <para>
        /// This is the exact inverse of what the export writes. <see cref="Modify.Update(TBD.profile, Profile, double)"/>
        /// splits a SAM gain into two independent halves:
        /// </para>
        /// <list type="bullet">
        /// <item><description><c>profile_TBD.factor</c> = the magnitude (the SAM parameter);</description></item>
        /// <item><description><c>profile_TBD.hourlyValues</c>/<c>value</c>/yearly values = the SAM
        /// <see cref="Profile"/>'s RAW values, i.e. the schedule SHAPE, copied across untouched.</description></item>
        /// </list>
        /// <para>
        /// The import must therefore read the factor back, and read the shape back separately
        /// (<c>SAM.Core.Tas.Query.Values</c>, which is likewise raw). Reading
        /// <c>GetExtremeValue(true)</c> - which is <c>factor * max(values)</c> - instead folds the
        /// schedule's peak into the magnitude, and the NEXT export writes that folded number back as the
        /// factor while re-applying the same values. One generation of a round trip then becomes
        /// </para>
        /// <code>G(n+1) = G(n) * max(values)</code>
        /// <para>
        /// which is a fixed point only for a schedule normalised to a peak of 1.0 - the usual TAS
        /// convention, and why this stayed invisible for so long. A TM59 occupancy schedule peaking at
        /// 0.25 decayed the authored occupancy gain by a factor of four every generation, without bound
        /// (measured: 0.5 -> 0.125 -> 0.03125 W/m2 over three licensed generations), while the lighting,
        /// equipment and infiltration profiles in the same model - all peaking at 1.0 - were stable.
        /// </para>
        /// <para>
        /// The schedule shape is deliberately NOT normalised to fix this: profile definitions are shared
        /// between internal conditions (see <see cref="ProfileReuseIndex"/>), so the peak is a property of
        /// the shared SHAPE and the magnitude is a property of the individual slot. Rescaling one to
        /// compensate for the other would mutate a reusable definition on behalf of one of its users.
        /// </para>
        /// <para>
        /// The same rule already governs <c>ticV</c>, where it was established by a licensed round trip
        /// that turned a 2.0 ACH source profile into 40.8 ACH - see the ticV block in
        /// <c>Convert.ToSAM(TBD.InternalCondition, double, ProfileReuseIndex)</c>.
        /// </para>
        /// </summary>
        /// <param name="profile">The TBD profile occupying an internal-gain slot.</param>
        /// <returns>The slot's magnitude, or <c>double.NaN</c> when there is no profile.</returns>
        public static double GainMagnitude(this TBD.profile profile)
        {
            if (profile == null)
            {
                return double.NaN;
            }

            return profile.factor;
        }

        /// <summary>
        /// TIC counterpart of <see cref="GainMagnitude(TBD.profile)"/>. A <c>.tic</c> internal-conditions
        /// library stores its profiles exactly as a <c>.tbd</c> does, and the export writes both through
        /// the same factor/values split, so the same rule applies verbatim.
        /// </summary>
        public static double GainMagnitude(this TIC.profile profile)
        {
            if (profile == null)
            {
                return double.NaN;
            }

            return profile.factor;
        }
    }
}
