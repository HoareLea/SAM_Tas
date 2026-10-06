// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The base an imported profile name falls back to when no usable source TAS profile name is available.
        /// </summary>
        public const string ProfileNameBase_Default = "Profile";

        /// <summary>
        /// How much of a source TAS profile name a generated SAM profile name will carry. A profile name is a UI
        /// label and is written back onto <c>TBD.profile.name</c> on export, so an unbounded one would make both
        /// lists unreadable. Truncation cannot cost correctness: two long names that truncate alike simply
        /// collide, and a collision is resolved by the discriminator below rather than by merging.
        /// </summary>
        public const int ProfileNameBaseLimit = 120;

        /// <summary>
        /// The canonical SAM library name for one reusable profile definition, given the names already claimed
        /// WITHIN THE SAME CATEGORY.
        /// <para>
        /// <b>No part of this name comes from a zone or an internal condition.</b> The base is the source TAS
        /// PROFILE's own name - a property of the reusable schedule, shared by every internal condition that
        /// carries it - and never <c>"{internal condition} [{profile}]"</c>, which is exactly what made one SAM
        /// profile per zone unavoidable. Naming and sharing are mutually exclusive when the name states a place.
        /// </para>
        /// <para>The rule, in order:</para>
        /// <list type="number">
        /// <item>the normalised preferred base (see <see cref="ProfileNameBase(string)"/>) - which the caller
        /// derives as the ordinal-smallest source name in the definition's equality group, so it does not depend
        /// on which internal condition was met first;</item>
        /// <item><c>&lt;base&gt;_&lt;signature hash&gt;</c> when the base is already claimed by a DIFFERENT
        /// definition - a deterministic collision suffix, never a UI-style <c>(1)</c>/<c>(2)</c> counter, so the
        /// same definition resolves to the same name on every repeated import;</item>
        /// <item><c>&lt;base&gt;_&lt;signature hash&gt;_&lt;k&gt;</c>, k counting up from 2, when even that is
        /// claimed - reachable only when the bounded signature fingerprint collides, or when another definition's
        /// own preferred base happens to be that exact string.</item>
        /// </list>
        /// <para>
        /// <b>It never refuses and never returns a name already claimed.</b> Every distinct definition gets a
        /// distinct name, so no valid profile is dropped and no existing definition is overwritten - unlike the
        /// aperture-type case, where refusing was correct because the alternative was writing over an object the
        /// export did not author. Here every candidate is a fresh library entry this import is creating.
        /// </para>
        /// <para>
        /// Determinism therefore rests entirely on the ORDER definitions are offered in, which is why
        /// <see cref="ProfileReuseIndex"/> claims them in <see cref="ProfileDefinition.CompareTo"/> order rather
        /// than encounter order.
        /// </para>
        /// </summary>
        /// <param name="claimedNames">Names already taken within this definition's category. Must compare ordinally.</param>
        /// <param name="profileDefinition">The definition being named.</param>
        /// <param name="preferred">The preferred source name, un-normalised. Null or unusable falls back to <see cref="ProfileNameBase_Default"/>.</param>
        public static string ProfileName(ICollection<string> claimedNames, ProfileDefinition profileDefinition, string preferred)
        {
            string @base = ProfileNameBase(preferred);
            if (claimedNames == null || !claimedNames.Contains(@base))
            {
                return @base;
            }

            string hash = ProfileSignatureHash(profileDefinition) ?? Fnv1aHex(@base);

            string candidate = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", @base, hash);
            if (!claimedNames.Contains(candidate))
            {
                return candidate;
            }

            //Bounded in practice by the number of definitions in one category: each iteration can only be
            //blocked by a distinct name already claimed by a distinct definition.
            for (int i = 2; ; i++)
            {
                candidate = string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}", @base, hash, i.ToString(CultureInfo.InvariantCulture));
                if (!claimedNames.Contains(candidate))
                {
                    return candidate;
                }
            }
        }

        /// <summary>
        /// The name a flattened HDD sizing profile carries: the design profile's name plus <c>" - HDD"</c>.
        /// <para>
        /// <c>Modify.UpdateInternalCondition_HDD</c> flattens the space condition's infiltration and heating
        /// profiles onto the HDD sizing condition as single-value <c>ticValueProfile</c>s. That content is a
        /// DIFFERENT definition from the full schedule it was derived from, so it must not carry the same name —
        /// sharing it is what forced the next import to discriminate a same-name/different-values pair and
        /// accreted one <c>_&lt;hash&gt;</c> suffix per SAM → TAS → SAM generation. The suffix matches the one the
        /// HDD condition itself already uses (<c>space.Name + " - HDD"</c>).
        /// </para>
        /// <para>
        /// A plain concatenation, deliberately: no formatting, no culture, no ordering. It lives here rather than
        /// inline at the two write sites so the COM-free naming test exercises the production rule instead of
        /// re-implementing it.
        /// </para>
        /// </summary>
        public static string ProfileName_HDD(string profileName)
        {
            return profileName + " - HDD";
        }

        /// <summary>
        /// The name base a source TAS profile name resolves to: trimmed, with internal whitespace collapsed,
        /// control characters removed, and length bounded by <see cref="ProfileNameBaseLimit"/>. An absent or
        /// unusable name falls back to <see cref="ProfileNameBase_Default"/>.
        /// <para>
        /// Underscores are kept, unlike <see cref="ApertureTypeNameBase(string)"/>: real TAS profile names carry
        /// them (<c>HTG_7to19_21</c>), and stripping them would mangle every heating and cooling setpoint name in
        /// a typical model. A generated name is therefore not required to be decomposable - uniqueness is
        /// guaranteed by the claim set in <see cref="ProfileName(ICollection{string}, ProfileDefinition, string)"/>,
        /// not by the grammar.
        /// </para>
        /// </summary>
        public static string ProfileNameBase(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ProfileNameBase_Default;
            }

            StringBuilder stringBuilder = new StringBuilder(name.Length);
            bool whitespace = false;
            foreach (char character in name.Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    whitespace = true;
                    continue;
                }

                if (char.IsControl(character))
                {
                    continue;
                }

                if (whitespace && stringBuilder.Length != 0)
                {
                    stringBuilder.Append(' ');
                }

                whitespace = false;
                stringBuilder.Append(character);
            }

            string result = stringBuilder.ToString().Trim();
            if (result.Length == 0)
            {
                return ProfileNameBase_Default;
            }

            return result.Length <= ProfileNameBaseLimit ? result : result.Substring(0, ProfileNameBaseLimit).TrimEnd();
        }

        /// <summary>
        /// The SAM <c>Profile.Category</c> string a raw category resolves to, mirroring exactly what the
        /// <see cref="Profile"/> constructor does with it: a category naming a <see cref="ProfileType"/> or a
        /// <see cref="ProfileGroup"/> is replaced by that enum's own text, and anything else is kept verbatim.
        /// <para>
        /// Applied on the way into <see cref="ProfileDefinition"/> so that a definition's category and the
        /// category of the <see cref="Profile"/> built from it are always the same string. That equality is what
        /// makes the <c>ProfileLibrary</c> key <c>"{Category}::{Name}"</c> - and therefore the per-category name
        /// claim set - predictable from the definition alone.
        /// </para>
        /// <para>
        /// This is a normalisation, not a widening: distinct categories stay distinct. The RAW category is what
        /// identity is keyed on, never merely the resolved <see cref="ProfileType"/>, so two categories that
        /// resolve to the same profile type but read differently remain two definitions.
        /// </para>
        /// </summary>
        public static string ProfileCategory(string category)
        {
            if (category == null)
            {
                return null;
            }

            ProfileType profileType = Core.Query.Enum<ProfileType>(category);
            if (profileType != ProfileType.Undefined)
            {
                return Analytical.Query.Text(profileType);
            }

            ProfileGroup profileGroup = Core.Query.Enum<ProfileGroup>(category);
            if (profileGroup != ProfileGroup.Undefined)
            {
                return Analytical.Query.Text(profileGroup);
            }

            return category;
        }
    }
}
