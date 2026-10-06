// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The base a generated aperture construction name falls back to when the SAM
        /// <c>ApertureConstruction</c> states no usable name.
        /// </summary>
        public const string ConstructionNameBase_Default = "Aperture Construction";

        /// <summary>
        /// How much of an <c>ApertureConstruction</c> name a generated construction name will carry. A TBD
        /// name is a UI label, and an unbounded one would make the construction list unreadable.
        /// </summary>
        public const int ConstructionNameBaseLimit = 80;

        //^<base>[_<collision hash>] -pane|-frame$
        //Anchored, and the part suffix is TERMINAL by construction - see the remarks on
        //ConstructionName about why the collision discriminator goes on the base and never on the tail.
        private static readonly Regex regex_ConstructionName = new Regex(
            @"^(?<base>.+?)(?:_(?<hash>[0-9A-F]{8}))? (?<sufix>-pane|-frame)$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// The name to create a NEW TBD aperture construction under, given the names already in the building.
        /// <para>
        /// <b>No part of this name comes from a physical aperture.</b> The base is the SAM
        /// <c>ApertureConstruction</c>'s own name - a reusable definition shared by every window built from
        /// it - where the previous naming used <c>aperture.UniqueName()</c>, which carries the aperture's
        /// GUID. A GUID-named construction can never be found again by the next identical window, so sharing
        /// and GUID-naming are mutually exclusive.
        /// </para>
        /// <para>
        /// <b>The shape matches what the TCD route already writes</b> (<c>Convert.ToTCD_Constructions</c>:
        /// <c>apertureConstruction.Name + " -pane"</c>), and it is the shape the import reads back: it strips
        /// the <c>-pane</c>/<c>-frame</c> suffix to recover the base the reconstructed
        /// <c>ApertureConstruction</c> is named after, and <c>Convert.ToSAM_ApertureConstruction</c> reads the
        /// suffix to decide which layer list a construction is. So the suffix must stay TERMINAL - which is
        /// why a collision discriminator is appended to the BASE, <c>Glazing_1F3A0C21 -pane</c>, and never to
        /// the tail. A name ending <c>-pane_1F3A0C21</c> would not be recognised as a pane at all.
        /// </para>
        /// <para>
        /// <b>This is only ever reached after a DEFINITION search has failed.</b> Definitions establish
        /// reuse; a name is metadata. So any existing construction sharing the preferred name necessarily
        /// holds different content (or content this export could not prove), and must not be adopted or
        /// overwritten - which is precisely the unsafe behaviour this replaces: the previous code took any
        /// construction of a matching name whatever its layers.
        /// </para>
        /// <para>The rule, in order:</para>
        /// <list type="number">
        /// <item><c>&lt;base&gt; -pane</c> / <c>&lt;base&gt; -frame</c>;</item>
        /// <item><c>&lt;base&gt;_&lt;signature hash&gt; -pane</c> when the preferred name is taken by
        /// different content - a deterministic collision suffix, never a TAS/UI-style <c>(1)</c>/<c>(2)</c>
        /// counter, so the same definition resolves to the same name on every repeated export;</item>
        /// <item>otherwise a refusal, rather than a third guess.</item>
        /// </list>
        /// </summary>
        /// <param name="existingNames">Every construction name already in the building, including any created earlier in this export.</param>
        /// <param name="constructionDefinition">The content the new construction will hold. Only its <see cref="ConstructionDefinition.AperturePart"/> and its signature are used here.</param>
        /// <param name="name_ApertureConstruction">The SAM <c>ApertureConstruction</c>'s name.</param>
        /// <param name="refusal">Why no name could be chosen, or null on success.</param>
        /// <returns>The name to create the construction under, or null when <paramref name="refusal"/> is set.</returns>
        public static string ConstructionName(IEnumerable<string> existingNames, ConstructionDefinition constructionDefinition, string name_ApertureConstruction, out string refusal)
        {
            refusal = null;

            if (constructionDefinition == null)
            {
                refusal = "No construction definition was supplied, so no construction name was derived and nothing was created.";
                return null;
            }

            string sufix = constructionDefinition.AperturePart.Sufix();
            if (string.IsNullOrWhiteSpace(sufix))
            {
                refusal = string.Format("An aperture construction is either a pane or a frame; '{0}' is neither, so no construction name was derived and nothing was created.", constructionDefinition.AperturePart);
                return null;
            }

            HashSet<string> names = new HashSet<string>(existingNames == null ? Enumerable.Empty<string>() : existingNames.Where(x => x != null));

            string @base = ConstructionNameBase(name_ApertureConstruction);

            string preferred = string.Format("{0} {1}", @base, sufix);
            if (!names.Contains(preferred))
            {
                return preferred;
            }

            //The discriminator goes on the BASE so that the part suffix stays terminal - see the remarks.
            string qualified = string.Format("{0}_{1} {2}", @base, ConstructionSignatureHash(constructionDefinition), sufix);
            if (!names.Contains(qualified))
            {
                return qualified;
            }

            refusal = string.Format("TBD construction '{0}' already exists with different content, and so does the signature-qualified alternative '{1}'. Rather than guess a third name or overwrite a construction this export did not author, nothing was written.", preferred, qualified);
            return null;
        }

        /// <summary>
        /// The name base an <c>ApertureConstruction</c> name resolves to: trimmed, with internal whitespace
        /// collapsed, characters that would break the name grammar removed, and length bounded. An absent or
        /// unusable name falls back to <see cref="ConstructionNameBase_Default"/>.
        /// </summary>
        public static string ConstructionNameBase(string name_ApertureConstruction)
        {
            if (string.IsNullOrWhiteSpace(name_ApertureConstruction))
            {
                return ConstructionNameBase_Default;
            }

            StringBuilder stringBuilder = new StringBuilder(name_ApertureConstruction.Length);
            bool whitespace = false;
            foreach (char character in name_ApertureConstruction.Trim())
            {
                if (char.IsWhiteSpace(character))
                {
                    whitespace = true;
                    continue;
                }

                //Only control characters are dropped. An underscore is KEPT, unlike in the aperture type
                //naming: real construction names are full of them - SIM_EXT_GLZ - and this base is the
                //round-trip identity of the SAM ApertureConstruction, which the aperture import rebuilds by
                //stripping the part suffix. Mangling it to SIMEXTGLZ would silently rename the construction
                //on every round trip. The underscore is still the collision discriminator's separator, so a
                //base that itself ends in eight hex digits after an underscore decomposes ambiguously - but
                //only in the BASE, which nothing reads: what decomposition is asked for is the part suffix
                //and the Windows:/Doors: prefix, and those stay unambiguous.
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
                return ConstructionNameBase_Default;
            }

            result = result.Length <= ConstructionNameBaseLimit ? result : result.Substring(0, ConstructionNameBaseLimit).TrimEnd();

            //A base that itself ends in a part suffix would produce "X -pane -pane" and, worse, decompose
            //ambiguously. Strip it: the suffix this export appends is the one that counts.
            foreach (Analytical.AperturePart aperturePart in new Analytical.AperturePart[] { Analytical.AperturePart.Pane, Analytical.AperturePart.Frame })
            {
                string sufix = aperturePart.Sufix();
                if (result.EndsWith(sufix, global::System.StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(0, result.Length - sufix.Length).TrimEnd();
                }
            }

            return result.Length == 0 ? ConstructionNameBase_Default : result;
        }

        /// <summary>
        /// Whether <paramref name="name"/> has the shape
        /// <see cref="ConstructionName(IEnumerable{string}, ConstructionDefinition, string, out string)"/>
        /// generates, and if so which half of a window it names.
        /// <para>
        /// <b>This is how a pre-existing construction's part is established at all.</b> TAS does not store
        /// "pane" or "frame" on a construction - the whole convention lives in the name, which is why the
        /// import reads it from there too. A construction whose name does not carry the convention therefore
        /// has an UNKNOWN part, and an unknown part is never reused: refusing costs one extra construction,
        /// where guessing could merge a frame into a pane and lose half a window on the round trip.
        /// </para>
        /// </summary>
        public static bool TryDecomposeConstructionName(string name, out string @base, out Analytical.AperturePart aperturePart)
        {
            @base = null;
            aperturePart = Analytical.AperturePart.Undefined;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            Match match = regex_ConstructionName.Match(name);
            if (!match.Success)
            {
                return false;
            }

            @base = match.Groups["base"].Value;

            string sufix = match.Groups["sufix"].Value;
            if (string.Equals(sufix, Analytical.AperturePart.Pane.Sufix(), global::System.StringComparison.Ordinal))
            {
                aperturePart = Analytical.AperturePart.Pane;
            }
            else if (string.Equals(sufix, Analytical.AperturePart.Frame.Sufix(), global::System.StringComparison.Ordinal))
            {
                aperturePart = Analytical.AperturePart.Frame;
            }

            return aperturePart != Analytical.AperturePart.Undefined;
        }
    }
}
