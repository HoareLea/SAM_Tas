// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Globalization;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>The one place a TBD -&gt; SAM import decides which SAM <see cref="Profile"/>s exist and what they are
    /// called.</b> Built once per conversion and threaded through every path that produces profile references,
    /// so the <c>ProfileLibrary</c> and every <c>InternalCondition</c> in the model agree by construction.
    /// <para>The architecture it implements:</para>
    /// <list type="bullet">
    /// <item>TBD zone-local internal-condition / profile slots, which stay exactly as many as TAS states;</item>
    /// <item>imported SAM <c>InternalCondition</c>s, likewise one per TBD internal condition;</item>
    /// <item>shared SAM <c>ProfileLibrary</c> definitions - as many as there are distinct
    /// <see cref="ProfileDefinition"/>s, and no more.</item>
    /// </list>
    /// <para>
    /// <b>Two passes, and the second one may not depend on the first one's order.</b> Registration
    /// (<see cref="Register"/>) walks the building and collects definitions with the source TAS names that want
    /// them; <see cref="Resolve"/> then assigns every definition its canonical name in
    /// <see cref="ProfileDefinition.CompareTo"/> order, which is derived from the definitions alone. Reverse the
    /// building walk, or run the import twice, and the same definitions get the same names.
    /// </para>
    /// <para>
    /// <b>Why every conversion path must share ONE instance.</b> The library is built from the definitions this
    /// index resolved. Any internal condition converted without it keeps the legacy
    /// <c>"{internal condition} [{profile}]"</c> reference, which after dedup names nothing in the library - a
    /// dangling reference. That is why <c>Modify.AddUnusedInternalConditions</c>, which converts the
    /// building-level template conditions no zone owns, takes the index too.
    /// </para>
    /// <para>
    /// <b>Slot lookup, and why there is a definitional fallback behind it.</b> The fast path answers
    /// <c>(internal condition name, TBD profile slot) -&gt; resolved name</c>, so the conversion pays no second
    /// COM read for values already read during registration. A name is not an identity, though: should two TBD
    /// internal conditions share a name and disagree on a slot, that slot key is marked ambiguous and answers
    /// nothing, and the caller falls back to <see cref="GetProfileName(string, IEnumerable{double})"/>, which is
    /// keyed on the definition itself and always can answer.
    /// </para>
    /// <para>
    /// <b>Zero-length definitions are excluded from dedup.</b> A TAS function profile reads back with no values,
    /// so its flattened form is an incomplete representation and merging by it would be unsafe. Those keep their
    /// existing per-internal-condition name and their existing library entry, unchanged; their names are claimed
    /// BEFORE any canonical name is assigned so that a canonical name can never displace one.
    /// </para>
    /// <para>
    /// <b>This class touches no COM type</b>, which is what makes the whole reuse and naming decision testable
    /// without an installed TAS. The TBD reads live in <see cref="Query.ProfileReuseIndex(TBD.Building)"/>.
    /// </para>
    /// </summary>
    public sealed class ProfileReuseIndex
    {
        private readonly Dictionary<ProfileDefinition, SortedSet<string>> sourceNames = new Dictionary<ProfileDefinition, SortedSet<string>>();
        private readonly Dictionary<string, ProfileDefinition> definitionsBySlot = new Dictionary<string, ProfileDefinition>(StringComparer.Ordinal);
        private readonly HashSet<string> ambiguousSlots = new HashSet<string>(StringComparer.Ordinal);

        //Slot key -> the name a zero-length (function) profile keeps, and the entries the library must still
        //carry for them. The slot map holds a name only for as long as the key means exactly ONE name; the same
        //key seen with a different one marks it ambiguous, exactly as the reusable path does. The library
        //entries are kept in registration order and deduped first-wins, which is safe: two entries sharing a
        //category and a name also share their (empty) values, so they are the same library entry either way.
        private readonly Dictionary<string, string> excludedNamesBySlot = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<Profile> excludedProfiles = new List<Profile>();
        private readonly HashSet<string> excludedKeys = new HashSet<string>(StringComparer.Ordinal);

        //Category -> names that must not be handed to a canonical definition, for slots that were skipped
        //entirely rather than excluded. See Reserve.
        private readonly Dictionary<string, HashSet<string>> reservedNamesByCategory = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        private Dictionary<ProfileDefinition, Profile> profiles;
        private List<Profile> resolvedProfiles;

        /// <summary>Whether <see cref="Resolve"/> has run. Every lookup requires it.</summary>
        public bool Resolved
        {
            get { return resolvedProfiles != null; }
        }

        /// <summary>The number of distinct reusable definitions collected. Excludes the zero-length ones.</summary>
        public int DefinitionCount
        {
            get { return sourceNames.Count; }
        }

        /// <summary>
        /// Every SAM profile this import should create: the resolved shared definitions, then the excluded
        /// zero-length ones. Empty until <see cref="Resolve"/> has run.
        /// </summary>
        public List<Profile> Profiles
        {
            get
            {
                List<Profile> result = new List<Profile>();

                if (!Resolved)
                {
                    return result;
                }

                result.AddRange(resolvedProfiles);
                result.AddRange(excludedProfiles);

                return result;
            }
        }

        /// <summary>
        /// Record one TBD internal-condition profile slot.
        /// </summary>
        /// <param name="internalConditionName">The TBD internal condition's name. Naming plays no part in identity; this is a lookup key only.</param>
        /// <param name="slot">The <c>TBD.Profiles</c> slot, as an int so this class stays COM-free.</param>
        /// <param name="category">The SAM profile category the slot maps to.</param>
        /// <param name="values">The complete flattened values read out of TAS.</param>
        /// <param name="sourceName">The source TAS profile's own name - a candidate for the canonical name, and nothing else.</param>
        /// <param name="excludedName">The name a zero-length (function) profile must keep, i.e. today's <c>"{internal condition} [{profile}]"</c>.</param>
        /// <param name="suppressLibraryEntry">
        /// True for a slot that must not become a resolvable library entry at all (today: a zero-length
        /// <c>ticV</c>, whose function semantics are deferred). The slot key still participates fully in
        /// ambiguity tracking below - two TBD internal conditions sharing a name, one with this slot excluded
        /// and the other registering it as a genuine reusable definition, still correctly mark the shared key
        /// ambiguous so NEITHER answers it - only the final library emission is skipped. Callers should also
        /// <see cref="Reserve"/> <paramref name="excludedName"/> to close the separate case where an unrelated
        /// definition's own canonical name happens to equal the string this suppressed slot would have
        /// answered.
        /// </param>
        /// <returns>True when the slot was collected as a REUSABLE definition, false when it was excluded or rejected.</returns>
        /// <remarks>
        /// Binary-compatible overload, kept exactly as it was before <c>suppressLibraryEntry</c> existed. An
        /// optional parameter is a COMPILE-time convenience only - it does not preserve the original CLR method,
        /// so a caller already compiled against this six-argument signature (a Grasshopper/Revit plugin
        /// referencing this DLL as a binary, not recompiled on every SAM_Tas release) would throw
        /// <see cref="MissingMethodException"/> the moment it loaded a build that replaced this overload with a
        /// seven-argument one, default value or not. Forwards to the real
        /// implementation with the library-suppression option off, i.e. identical to this method's previous body.
        /// </remarks>
        public bool Register(string internalConditionName, int slot, string category, IEnumerable<double> values, string sourceName, string excludedName)
        {
            return Register(internalConditionName, slot, category, values, sourceName, excludedName, suppressLibraryEntry: false);
        }

        /// <returns>True when the slot was collected as a REUSABLE definition, false when it was excluded, suppressed or rejected.</returns>
        public bool Register(string internalConditionName, int slot, string category, IEnumerable<double> values, string sourceName, string excludedName, bool suppressLibraryEntry)
        {
            if (Resolved)
            {
                throw new InvalidOperationException("A ProfileReuseIndex cannot be registered into after it has been resolved.");
            }

            ProfileDefinition profileDefinition = new ProfileDefinition(category, values);
            string key = SlotKey(internalConditionName, slot);

            if (!profileDefinition.IsReusable)
            {
                //Out of scope for dedup - see the class remarks. Today's import behaviour is kept verbatim: the
                //profile keeps its per-internal-condition name and its own library entry.
                if (string.IsNullOrWhiteSpace(excludedName))
                {
                    return false;
                }

                if (definitionsBySlot.ContainsKey(key))
                {
                    MarkAmbiguous(key);
                }
                else if (!ambiguousSlots.Contains(key))
                {
                    if (excludedNamesBySlot.TryGetValue(key, out string existingExcludedName))
                    {
                        if (!string.Equals(existingExcludedName, excludedName, StringComparison.Ordinal))
                        {
                            //Two TBD internal conditions share a name and disagree on this slot's zero-length
                            //profile, so the key stands for two different legacy names. Answering EITHER would
                            //be a wrong reference on the other - a silent misreference, not a dangling one.
                            MarkAmbiguous(key);
                        }
                    }
                    else
                    {
                        excludedNamesBySlot[key] = excludedName;
                    }
                }

                //The library entry is added whether or not the slot key can answer: when it cannot, the caller
                //falls back to the legacy name, which is exactly what this entry is called. UNLESS the caller
                //has asked this slot to stay unresolvable altogether (a zero-length ticV) - then no entry is
                //emitted, but everything above (the ambiguity tracking) has already run exactly as it would
                //for any other excluded slot.
                if (!suppressLibraryEntry && excludedKeys.Add(string.Format(CultureInfo.InvariantCulture, "{0}::{1}", profileDefinition.Category, excludedName)))
                {
                    excludedProfiles.Add(new Profile(excludedName, profileDefinition.Category, profileDefinition.Values));
                }

                return false;
            }

            if (!sourceNames.TryGetValue(profileDefinition, out SortedSet<string> names))
            {
                names = new SortedSet<string>(StringComparer.Ordinal);
                sourceNames[profileDefinition] = names;
            }

            names.Add(Query.ProfileNameBase(sourceName));

            if (excludedNamesBySlot.ContainsKey(key))
            {
                //One slot key cannot both name a shared definition and name a zero-length passthrough.
                MarkAmbiguous(key);
            }
            else if (definitionsBySlot.TryGetValue(key, out ProfileDefinition existing))
            {
                if (!existing.Equals(profileDefinition))
                {
                    //Two TBD internal conditions share a name and disagree on this slot.
                    MarkAmbiguous(key);
                }
            }
            else if (!ambiguousSlots.Contains(key))
            {
                definitionsBySlot[key] = profileDefinition;
            }

            return true;
        }

        /// <summary>
        /// Claim a name in a category WITHOUT collecting anything under it - no definition, no library entry,
        /// and no slot able to answer it.
        /// <para>
        /// For slots the caller skips entirely rather than excludes: a zero-length (TAS function) <c>ticV</c>,
        /// whose reference is deliberately left dangling until function semantics exist. The dangling reference
        /// still HAS a name - the legacy <c>"{internal condition} [{profile}]"</c> the conversion falls back to -
        /// and if a canonical name were later assigned that exact string in the same category, the reference
        /// would stop dangling and start resolving to an unrelated value profile, which the export would then
        /// write over the function profile. Reserving the name keeps
        /// <see cref="Query.ProfileName(ICollection{string}, ProfileDefinition, string)"/> away from it, so the
        /// reference resolves to nothing, exactly as intended. Realistic rather than theoretical: a
        /// round-tripped model's TAS profile names ARE <c>"{condition} [{profile}]"</c> strings, because that is
        /// what the export writes back.
        /// </para>
        /// </summary>
        public void Reserve(string category, string name)
        {
            if (Resolved)
            {
                throw new InvalidOperationException("A ProfileReuseIndex cannot be reserved into after it has been resolved.");
            }

            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            if (!reservedNamesByCategory.TryGetValue(category, out HashSet<string> names))
            {
                names = new HashSet<string>(StringComparer.Ordinal);
                reservedNamesByCategory[category] = names;
            }

            names.Add(name);
        }

        /// <summary>
        /// Assign every collected definition its canonical SAM library name and build the shared
        /// <see cref="Profile"/>s. Idempotent; further <see cref="Register"/> calls are refused afterwards.
        /// </summary>
        public void Resolve()
        {
            if (Resolved)
            {
                return;
            }

            profiles = new Dictionary<ProfileDefinition, Profile>();
            resolvedProfiles = new List<Profile>();

            Dictionary<string, HashSet<string>> claimedByCategory = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            //The excluded zero-length names are claimed first, so a canonical name can never displace one.
            foreach (Profile profile in excludedProfiles)
            {
                Claimed(claimedByCategory, profile.Category).Add(profile.Name);
            }

            //So are the names reserved for skipped slots, for the same reason - see Reserve.
            foreach (KeyValuePair<string, HashSet<string>> keyValuePair in reservedNamesByCategory)
            {
                foreach (string name in keyValuePair.Value)
                {
                    Claimed(claimedByCategory, keyValuePair.Key).Add(name);
                }
            }

            List<ProfileDefinition> profileDefinitions = new List<ProfileDefinition>(sourceNames.Keys);
            profileDefinitions.Sort();

            foreach (ProfileDefinition profileDefinition in profileDefinitions)
            {
                HashSet<string> claimed = Claimed(claimedByCategory, profileDefinition.Category);

                //SortedSet<string> over StringComparer.Ordinal, so Min IS the ordinal-smallest normalised source
                //name - the same one whichever order the building was walked in.
                SortedSet<string> names = sourceNames[profileDefinition];
                string preferred = names.Count == 0 ? null : names.Min;

                string name = Query.ProfileName(claimed, profileDefinition, preferred);
                claimed.Add(name);

                Profile profile = new Profile(name, profileDefinition.Category, profileDefinition.Values);
                profiles[profileDefinition] = profile;
                resolvedProfiles.Add(profile);
            }
        }

        /// <summary>The shared SAM profile a definition resolves to, or null when it was never collected.</summary>
        public Profile GetProfile(string category, IEnumerable<double> values)
        {
            if (!Resolved)
            {
                return null;
            }

            return profiles.TryGetValue(new ProfileDefinition(category, values), out Profile profile) ? profile : null;
        }

        /// <summary>
        /// The canonical SAM library name a definition resolves to, or null when it was never collected. Keyed
        /// on the definition itself, so it always answers for anything the index holds.
        /// </summary>
        public string GetProfileName(string category, IEnumerable<double> values)
        {
            return GetProfile(category, values)?.Name;
        }

        /// <summary>
        /// The canonical SAM library name for one TBD internal-condition profile slot, without re-reading its
        /// values over COM. Null when the slot was never registered or when its key is ambiguous - the caller
        /// must then fall back to <see cref="GetProfileName(string, IEnumerable{double})"/>.
        /// </summary>
        public string GetProfileName(string internalConditionName, int slot)
        {
            if (!Resolved)
            {
                return null;
            }

            string key = SlotKey(internalConditionName, slot);

            if (excludedNamesBySlot.TryGetValue(key, out string excludedName))
            {
                return excludedName;
            }

            if (!definitionsBySlot.TryGetValue(key, out ProfileDefinition profileDefinition))
            {
                return null;
            }

            return profiles.TryGetValue(profileDefinition, out Profile profile) ? profile.Name : null;
        }

        /// <summary>
        /// Stop a slot key answering at all, permanently. Reached only when one key would have to stand for two
        /// different things - two TBD internal conditions sharing a name and disagreeing on the slot, or a slot
        /// that is a shared definition on one condition and a zero-length passthrough on another. The key
        /// answering EITHER would be a wrong reference on the other; answering nothing sends both callers to the
        /// definitional lookup, which is right for both.
        /// </summary>
        private void MarkAmbiguous(string key)
        {
            definitionsBySlot.Remove(key);
            excludedNamesBySlot.Remove(key);
            ambiguousSlots.Add(key);
        }

        private static HashSet<string> Claimed(Dictionary<string, HashSet<string>> claimedByCategory, string category)
        {
            string key = category ?? string.Empty;

            if (!claimedByCategory.TryGetValue(key, out HashSet<string> result))
            {
                result = new HashSet<string>(StringComparer.Ordinal);
                claimedByCategory[key] = result;
            }

            return result;
        }

        //A null separator, so no internal-condition name can be built to look like another name plus a slot.
        private static string SlotKey(string internalConditionName, int slot)
        {
            return string.Concat(internalConditionName ?? string.Empty, "\u0000", slot.ToString(CultureInfo.InvariantCulture));
        }
    }
}
