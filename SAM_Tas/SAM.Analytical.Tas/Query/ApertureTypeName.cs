// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The base a generated aperture type name falls back to when the opening states no description.
        /// </summary>
        public const string ApertureTypeNameBase_Default = "Opening";

        /// <summary>
        /// How much of a description a generated aperture type name will carry. A TBD name is a UI label,
        /// and an unbounded one would make the type list unreadable; the discriminating part of the name is
        /// the Cd/factor/schedule tail, which is always appended in full.
        /// </summary>
        public const int ApertureTypeNameBaseLimit = 60;

        //^base Cd<cd> F<factor>[ S<schedule signature>][ <ordinal>][_<collision hash>]$
        //Anchored, so a name either IS one this export generates or is not; see IsApertureTypeName.
        private static readonly Regex regex_ApertureTypeName = new Regex(
            @"^(?<base>.+?) Cd(?<cd>-?\d+(?:\.\d+)?) F(?<factor>-?\d+(?:\.\d+)?)(?: S(?<schedule>[0-9A-F]{6}|X[0-9A-F]{8}))?(?: (?<ordinal>\d+))?(?:_(?<hash>[0-9A-F]{8}))?$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// The name to create a NEW TBD aperture type under, given the names already in the building.
        /// <para>
        /// <b>No part of this name comes from a physical aperture.</b> The base is the opening's own
        /// DESCRIPTION - a property of the reusable control, shared by every window that carries it - and
        /// never the building element's name, which encodes the SAM aperture's GUID and is exactly what
        /// made one type per window unavoidable. A name that embedded an aperture GUID could not be found
        /// again by the next identical window, so sharing and GUID-naming are mutually exclusive.
        /// </para>
        /// <para>
        /// <b>This is only ever reached after a DEFINITION search has failed</b> - see
        /// <see cref="ApertureTypeIndex(IEnumerable{ApertureTypeDefinition}, ApertureTypeDefinition)"/>.
        /// Definitions establish reuse; a name is metadata. So any existing type sharing the preferred name
        /// necessarily has a different definition (or one this export may not reuse), and must not be
        /// overwritten.
        /// </para>
        /// <para>The rule, in order:</para>
        /// <list type="number">
        /// <item>
        /// <c>&lt;base&gt; Cd&lt;cd&gt; F&lt;factor&gt;[ S&lt;schedule signature&gt;]</c>, plus
        /// <c> &lt;ordinal&gt;</c> when this is the second or later occurrence of the same control on one
        /// element - continuing the <c>" 1"</c>/<c>" 2"</c> convention the previous per-element naming used;
        /// </item>
        /// <item>
        /// <c>&lt;that&gt;_&lt;signature hash&gt;</c> when the preferred name is taken by a different
        /// definition - a deterministic collision suffix, never a TAS/UI-style <c>(1)</c>/<c>(2)</c>
        /// counter, so the same definition resolves to the same name on every repeated export;
        /// </item>
        /// <item>otherwise a refusal, rather than a third guess.</item>
        /// </list>
        /// </summary>
        /// <param name="ordinal">
        /// The 1-based occurrence of this definition among one building element's opening children. Two
        /// identical children on one element are two occurrences and get two distinct types - TAS collapses
        /// a type assigned twice to one element into a single opening - but those ordinal types are
        /// themselves shared across every other element that needs them.
        /// </param>
        /// <param name="refusal">Why no name could be chosen, or null on success.</param>
        /// <returns>The name to create the aperture type under, or null when <paramref name="refusal"/> is set.</returns>
        public static string ApertureTypeName(IEnumerable<string> existingNames, ApertureTypeDefinition apertureTypeDefinition, int ordinal, out string refusal)
        {
            refusal = null;

            string signature = ApertureTypeSignature(apertureTypeDefinition);
            if (signature == null)
            {
                refusal = "No aperture type definition was supplied, so no aperture type name was derived and nothing was created.";
                return null;
            }

            HashSet<string> names = new HashSet<string>(existingNames == null ? Enumerable.Empty<string>() : existingNames.Where(x => x != null));

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(ApertureTypeNameBase(apertureTypeDefinition.Description));
            stringBuilder.AppendFormat(" Cd{0}", apertureTypeDefinition.DischargeCoefficient.ToString(ApertureTypeNumberFormat, CultureInfo.InvariantCulture));
            stringBuilder.AppendFormat(" F{0}", apertureTypeDefinition.Factor.ToString(ApertureTypeNumberFormat, CultureInfo.InvariantCulture));

            if (apertureTypeDefinition.HasSchedule)
            {
                string signature_Schedule = ScheduleSignature(apertureTypeDefinition.ScheduleValues);
                if (signature_Schedule != null)
                {
                    stringBuilder.AppendFormat(" S{0}", signature_Schedule);
                }
            }

            if (ordinal >= 2)
            {
                stringBuilder.AppendFormat(" {0}", ordinal);
            }

            string preferred = stringBuilder.ToString();
            if (!names.Contains(preferred))
            {
                return preferred;
            }

            string qualified = string.Format("{0}_{1}", preferred, Fnv1aHex(signature));
            if (!names.Contains(qualified))
            {
                return qualified;
            }

            refusal = string.Format("TBD aperture type '{0}' already exists with a different opening control, and so does the signature-qualified alternative '{1}'. Rather than guess a third name or overwrite an aperture type this export did not author, nothing was written.", preferred, qualified);
            return null;
        }

        /// <summary>
        /// The name base a description resolves to: trimmed, with internal whitespace collapsed, characters
        /// that would break the name grammar removed, and length bounded. An absent or unusable description
        /// falls back to <see cref="ApertureTypeNameBase_Default"/>.
        /// </summary>
        public static string ApertureTypeNameBase(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return ApertureTypeNameBase_Default;
            }

            StringBuilder stringBuilder = new StringBuilder(description.Length);
            bool whitespace = false;
            foreach (char character in description.Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    whitespace = true;
                    continue;
                }

                //An underscore is the collision discriminator's own separator, so it is not allowed to
                //arrive from a description and make a generated name ambiguous to read.
                if (char.IsControl(character) || character == '_')
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
                return ApertureTypeNameBase_Default;
            }

            return result.Length <= ApertureTypeNameBaseLimit ? result : result.Substring(0, ApertureTypeNameBaseLimit).TrimEnd();
        }

        /// <summary>
        /// Whether <paramref name="name"/> has the shape
        /// <see cref="ApertureTypeName(IEnumerable{string}, ApertureTypeDefinition, int, out string)"/>
        /// generates, and if so what ordinal it carries.
        /// <para>
        /// This is a NAME test and proves only who is likely to have written the name. It never decides
        /// reuse - reuse is full definitional equality. What it does decide is how an aperture type already
        /// assigned to a building element is treated when it does not provide the requested control: a name
        /// of this shape is a shared type this export authored, so a second one is refused rather than
        /// appended (which would double the ventilation), whereas an unrecognised name is left alone as
        /// somebody else's work.
        /// </para>
        /// </summary>
        public static bool TryDecomposeApertureTypeName(string name, out string @base, out int ordinal)
        {
            @base = null;
            ordinal = 1;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            Match match = regex_ApertureTypeName.Match(name);
            if (!match.Success)
            {
                return false;
            }

            @base = match.Groups["base"].Value;

            Group group = match.Groups["ordinal"];
            if (group.Success && int.TryParse(group.Value, NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value >= 1)
            {
                ordinal = value;
            }

            return true;
        }

        /// <summary>
        /// Whether <paramref name="name"/> is a name the previous per-element convention would have
        /// produced for <paramref name="buildingElementName"/>: the element's own name, or the element's
        /// name followed by a child index.
        /// <para>
        /// Such a name is proof of EXCLUSIVITY, not of content: the element name carries the SAM aperture's
        /// GUID, so a type named after it can belong to no other element. That is what makes the legacy
        /// in-place write safe to keep exactly as it is on those types, and it is the only place in this
        /// work where an existing aperture type is written to.
        /// </para>
        /// </summary>
        public static bool IsLegacyApertureTypeName(string name, string buildingElementName)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(buildingElementName))
            {
                return false;
            }

            if (string.Equals(name, buildingElementName, global::System.StringComparison.Ordinal))
            {
                return true;
            }

            if (name.Length <= buildingElementName.Length + 1 || !name.StartsWith(buildingElementName, global::System.StringComparison.Ordinal) || name[buildingElementName.Length] != ' ')
            {
                return false;
            }

            return int.TryParse(name.Substring(buildingElementName.Length + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index >= 1;
        }
    }
}
