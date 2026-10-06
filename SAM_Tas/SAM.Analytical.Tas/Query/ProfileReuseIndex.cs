// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// Read every TBD internal-condition profile slot in <paramref name="building"/> once, and resolve the
        /// shared SAM profile definitions and names the import will use.
        /// <para>
        /// This is the ONLY place the values are read over COM to BUILD the index. The resulting
        /// <see cref="Analytical.Tas.ProfileReuseIndex"/> then answers both the <c>ProfileLibrary</c> build and
        /// every <c>InternalCondition</c> conversion from that one read, and no path can disagree with the
        /// library about what a profile reference names.
        /// </para>
        /// <para>
        /// One exception pays a second COM read: an ambiguous slot cannot answer from
        /// <see cref="Analytical.Tas.ProfileReuseIndex.GetProfileName(string, int)"/>, so the conversion falls
        /// back to <see cref="Analytical.Tas.ProfileReuseIndex.GetProfileName(string, System.Collections.Generic.IEnumerable{double})"/>,
        /// which re-marshals the same <c>TBD.profile</c>'s values the conversion already holds a handle to. That
        /// path is correct - it is keyed on the definition itself and always answers - just not free of a
        /// second read.
        /// </para>
        /// <para>
        /// <b>The slot set is exactly the one the legacy <c>Convert.ToSAM_Profiles</c> emits</b> - the eight
        /// internal-gain slots and the four thermostat slots, <c>ticV</c> (ventilation) included. Collecting
        /// ticV is what makes the imported <c>InternalConditionParameter.VentilationProfileName</c> resolve:
        /// the import has always written that reference but never emitted the profile behind it, so it dangled
        /// until the slot joined the reusable set.
        /// </para>
        /// </summary>
        /// <returns>A resolved index, or null when there is nothing to read.</returns>
        public static ProfileReuseIndex ProfileReuseIndex(this TBD.Building building)
        {
            List<TBD.InternalCondition> internalConditions_TBD = InternalConditions(building);
            if (internalConditions_TBD == null)
            {
                return null;
            }

            ProfileReuseIndex result = new ProfileReuseIndex();

            foreach (TBD.InternalCondition internalCondition_TBD in internalConditions_TBD)
            {
                Register(result, internalCondition_TBD);
            }

            result.Resolve();

            return result;
        }

        /// <summary>
        /// The <c>TBD.Profiles</c> slots an internal gain contributes to the reusable profile set, and the SAM
        /// <see cref="ProfileType"/> each maps to. Mirrors <c>Convert.ToSAM_Profiles</c> exactly.
        /// </summary>
        internal static readonly KeyValuePair<TBD.Profiles, ProfileType>[] ProfileSlots_InternalGain =
        {
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticI, ProfileType.Infiltration),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticV, ProfileType.Ventilation),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticLG, ProfileType.Lighting),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticOLG, ProfileType.Occupancy),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticOSG, ProfileType.Occupancy),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticESG, ProfileType.EquipmentSensible),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticELG, ProfileType.EquipmentLatent),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticCOG, ProfileType.Pollutant),
        };

        /// <summary>
        /// The <c>TBD.Profiles</c> slots a thermostat contributes, and the SAM <see cref="ProfileType"/> each maps
        /// to. Mirrors <c>Convert.ToSAM_Profiles</c> exactly.
        /// </summary>
        internal static readonly KeyValuePair<TBD.Profiles, ProfileType>[] ProfileSlots_Thermostat =
        {
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticUL, ProfileType.Cooling),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticLL, ProfileType.Heating),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticHLL, ProfileType.Humidification),
            new KeyValuePair<TBD.Profiles, ProfileType>(TBD.Profiles.ticHUL, ProfileType.Dehumidification),
        };

        /// <summary>
        /// The name an imported profile carries when it is NOT a shared definition - today's
        /// <c>"{internal condition} [{profile}]"</c>. Still the name of every zero-length (TAS function) profile,
        /// of the un-emitted ventilation reference, and of every profile produced by a conversion given no index.
        /// </summary>
        internal static string ProfileName_Legacy(string internalConditionName, string profileName)
        {
            return string.Format("{0} [{1}]", internalConditionName, profileName);
        }

        private static void Register(ProfileReuseIndex profileReuseIndex, TBD.InternalCondition internalCondition_TBD)
        {
            if (profileReuseIndex == null || internalCondition_TBD == null)
            {
                return;
            }

            string name = internalCondition_TBD.name;

            TBD.InternalGain internalGain = internalCondition_TBD.GetInternalGain();
            if (internalGain != null)
            {
                foreach (KeyValuePair<TBD.Profiles, ProfileType> keyValuePair in ProfileSlots_InternalGain)
                {
                    Register(profileReuseIndex, name, internalGain.GetProfile((int)keyValuePair.Key), keyValuePair.Key, keyValuePair.Value);
                }
            }

            TBD.Thermostat thermostat = internalCondition_TBD.GetThermostat();
            if (thermostat != null)
            {
                foreach (KeyValuePair<TBD.Profiles, ProfileType> keyValuePair in ProfileSlots_Thermostat)
                {
                    Register(profileReuseIndex, name, thermostat.GetProfile((int)keyValuePair.Key), keyValuePair.Key, keyValuePair.Value);
                }
            }
        }

        /// <summary>
        /// Whether a slot may be collected at all — i.e. whether the flattened <paramref name="values"/> are a
        /// COMPLETE representation of the TAS profile behind it.
        /// <para>
        /// This matters only for <c>ticV</c>, and only because collecting it is new. <c>Core.Tas.Query.Values</c>
        /// has no case for <c>ticFunctionProfile</c>, so a TAS function profile flattens to ZERO values. For every
        /// other slot a zero-length profile takes PR #37's exclusion path: no dedup, but still its own
        /// legacy-named library entry — harmless there, because those references already resolved and already
        /// round-tripped that way. For <c>ticV</c> that entry would be new, and it would make
        /// <c>VentilationProfileName</c> resolve to a zero-value profile for the first time. The export would
        /// then hand it to the ordinary value writer, where <c>Modify.Update</c>'s <c>Count == -1</c> guard misses
        /// <c>Count == 0</c> and the <c>Count &lt;= 24</c> branch overwrites the function profile with 24 hourly
        /// values. Refusing to collect it keeps the reference dangling exactly as it did before PR #38, which is
        /// the safe deferred behaviour until function semantics are implemented on their own.
        /// </para>
        /// <para>
        /// Slot is an <c>int</c> so the guard is reachable from the COM-free test project
        /// (<c>ZeroLength_Ventilation_IsNotCollectable_SoItCannotBecomeAResolvableValueProfile</c>).
        /// </para>
        /// </summary>
        internal static bool IsCollectableSlot(int slot, IEnumerable<double> values)
        {
            if (slot != (int)TBD.Profiles.ticV)
            {
                return true;
            }

            if (values == null)
            {
                return false;
            }

            using (IEnumerator<double> enumerator = values.GetEnumerator())
            {
                return enumerator.MoveNext();
            }
        }

        private static void Register(ProfileReuseIndex profileReuseIndex, string internalConditionName, TBD.profile profile_TBD, TBD.Profiles slot, ProfileType profileType)
        {
            if (profile_TBD == null)
            {
                return;
            }

            List<double> values = Core.Tas.Query.Values(profile_TBD);
            string category = Analytical.Query.Text(profileType);
            string name_Legacy = ProfileName_Legacy(internalConditionName, profile_TBD.name);
            bool collectable = IsCollectableSlot((int)slot, values);

            if (!collectable)
            {
                //Not collected: no library entry is emitted for this slot, so the conversion falls back to
                //the legacy name and the reference dangles - the safe deferred behaviour. RESERVE that name
                //so Resolve cannot later hand the identical string to an UNRELATED condition's canonical
                //definition (two different internal conditions, no slot-key overlap, a coincidental string
                //collision - Register's own ambiguity tracking below cannot see this, because it never
                //shares a key with the skipped slot).
                profileReuseIndex.Reserve(category, name_Legacy);
            }

            //Still registered even when not collectable, with the library entry suppressed: two TBD internal
            //conditions can share a name (a duplicate space name, a generic template name) with one carrying
            //this slot as a genuine reusable definition and the other as a zero-length function profile. Both
            //then compete for the SAME slot key, and only Register's ambiguity tracking - shared with every
            //other excluded slot - can mark that key unanswerable so the function condition's reference does
            //not resolve to the ordinary condition's profile.
            profileReuseIndex.Register(
                internalConditionName,
                (int)slot,
                category,
                values,
                profile_TBD.name,
                name_Legacy,
                suppressLibraryEntry: !collectable);
        }
    }
}
