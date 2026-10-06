// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        /// <summary>
        /// The legacy no-index profile collector: one SAM <see cref="Profile"/> per TBD internal-condition
        /// profile slot, named <c>"{internal condition} [{profile}]"</c>.
        /// <para>
        /// <b>Driven by the SAME slot tables as the reuse index</b>
        /// (<c>Query.ProfileSlots_InternalGain</c> / <c>Query.ProfileSlots_Thermostat</c>) and gated by the same
        /// <c>Query.IsCollectableSlot</c> predicate, so the two collectors cannot drift apart: a slot added to
        /// one is emitted by both, and a slot refused by one is refused by both. It used to repeat the twelve
        /// slots by hand, which is exactly how <c>ticV</c> could have been emitted here without being collected
        /// there (or the reverse) - a reference naming a definition the library does not carry.
        /// </para>
        /// </summary>
        public static List<Profile> ToSAM_Profiles(this TBD.InternalCondition internalCondition)
        {
            if(internalCondition == null)
            {
                return null;
            }

            List<Profile> result = new List<Profile>();

            TBD.InternalGain internalGain = internalCondition.GetInternalGain();
            if(internalGain != null)
            {
                foreach (KeyValuePair<TBD.Profiles, ProfileType> keyValuePair in Query.ProfileSlots_InternalGain)
                {
                    Add(result, ToSAM(internalGain, keyValuePair.Key, keyValuePair.Value, internalCondition.name), keyValuePair.Key);
                }
            }

            TBD.Thermostat thermostat = internalCondition.GetThermostat();
            if(thermostat != null)
            {
                foreach (KeyValuePair<TBD.Profiles, ProfileType> keyValuePair in Query.ProfileSlots_Thermostat)
                {
                    Add(result, ToSAM(thermostat, keyValuePair.Key, keyValuePair.Value, internalCondition.name), keyValuePair.Key);
                }
            }

            return result;
        }

        //One emission decision, shared with the reuse index. A slot whose flattened values are not a complete
        //representation of the TAS profile (today: a zero-length ticV, i.e. a function profile) is left out
        //entirely, so nothing in the library can claim its reference and the ordinary value exporter can never
        //be handed it. See Query.IsCollectableSlot.
        private static void Add(List<Profile> profiles, Profile profile, TBD.Profiles slot)
        {
            if (profile == null)
            {
                return;
            }

            if (!Query.IsCollectableSlot((int)slot, profile.GetValues()))
            {
                return;
            }

            profiles.Add(profile);
        }

        public static List<Profile> ToSAM_Profiles(this TBD.Building building)
        {
            List<TBD.InternalCondition> internalConditions_TBD = building?.InternalConditions();
            if(internalConditions_TBD == null|| internalConditions_TBD.Count == 0)
            {
                return null;
            }

            List<Profile> result = new List<Profile>();
            foreach(TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
            {
                List<Profile> profiles = internalCondition_TBD?.ToSAM_Profiles();
                if(profiles == null || profiles.Count ==0 )
                {
                    continue;
                }

                profiles.ForEach(x => result.Add(x));
            }


            return result;
        }
    }
}
