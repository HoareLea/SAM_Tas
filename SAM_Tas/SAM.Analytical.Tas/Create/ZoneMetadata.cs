// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        /// <summary>
        /// The <see cref="SAMZoneMetadata"/> for a space that has just been written into
        /// <paramref name="internalCondition_TBD"/> - the authored SAM airflow requirement, plus a fingerprint
        /// of the native TAS fields as the export left them.
        /// <para>
        /// The native halves are READ BACK off the TBD internal condition rather than recomputed, so the
        /// fingerprint is exactly what a later import will compare against: the singles TAS now holds, after
        /// the export's own conversions and after whatever the internal-condition template supplied for a
        /// field the export did not write.
        /// </para>
        /// <para>
        /// <b>Recording is not activating.</b> This is written whether or not a Ventilation profile is
        /// assigned - the four bases are engineering requirement data and survive either way -
        /// and <see cref="SAMZoneMetadata.VentilationProfileApplied"/> records WHICH of those two it was. That
        /// flag is the whole requirement/realisation distinction as it crosses the seam: only where it is set
        /// did SAM author the <c>ticV</c> rate, and only then is that rate fingerprinted or believed.
        /// </para>
        /// </summary>
        public static SAMZoneMetadata ZoneMetadata(this Space space, TBD.InternalCondition internalCondition_TBD, ProfileLibrary profileLibrary)
        {
            InternalCondition internalCondition = space?.InternalCondition;
            if (internalCondition == null)
            {
                return null;
            }

            SAMZoneMetadata result = new SAMZoneMetadata
            {
                SupplyAirFlow = AuthoredValue(internalCondition, InternalConditionParameter.SupplyAirFlow),
                SupplyAirFlowPerArea = AuthoredValue(internalCondition, InternalConditionParameter.SupplyAirFlowPerArea),
                SupplyAirFlowPerPerson = AuthoredValue(internalCondition, InternalConditionParameter.SupplyAirFlowPerPerson),
                SupplyAirChangesPerHour = AuthoredValue(internalCondition, InternalConditionParameter.SupplyAirChangesPerHour),
                VentilationProfileApplied = internalCondition.GetProfile(ProfileType.Ventilation, profileLibrary) != null,
            };

            TBD.InternalGain internalGain = internalCondition_TBD?.GetInternalGain();
            if (internalGain != null)
            {
                result.FreshAirRate = internalGain.freshAirRate;

                if (result.VentilationProfileApplied)
                {
                    TBD.profile profile_TBD = internalGain.GetProfile((int)TBD.Profiles.ticV);
                    if (profile_TBD != null)
                    {
                        result.VentilationFactor = profile_TBD.factor;
                    }
                }
            }

            return result;
        }

        private static double AuthoredValue(InternalCondition internalCondition, InternalConditionParameter internalConditionParameter)
        {
            if (!internalCondition.TryGetValue(internalConditionParameter, out double result))
            {
                return double.NaN;
            }

            return result;
        }
    }
}
