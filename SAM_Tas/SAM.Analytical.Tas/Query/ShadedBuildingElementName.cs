// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The name for a shade-stated member's OWN building element - the collision-safe variant of
        /// <see cref="BuildingElementName(IEnumerable{string}, BuildingElementDefinition, string, out string)"/>
        /// for elements that are deliberately never reused.
        /// <para>
        /// <b>Why this exists.</b> The ordinary naming carries a two-name budget - the preferred name, then
        /// the signature-qualified one - and the signature excludes the feature shade, because a shade is no
        /// part of a SHAREABLE definition (a shade-carrying element fails the seed gate). But a shade-stated
        /// split creates exactly such a non-shareable element, so a second shade split of the same
        /// definition derives a name that is already taken - and an already-split pane whose shade changes
        /// again collides with its own previous element. Both would come back null and leave the member
        /// bound to an element whose shade no longer matches.
        /// </para>
        /// <para>
        /// <b>The fallback is shade-content identity, then a counter.</b> The stamp, not the name, carries
        /// such an element's identity, so the name only has to be unique - but it is derived deterministically
        /// (same definition, same shade, same building state - same name) and keeps the
        /// <c>Windows: /Doors: &lt;base&gt;_&lt;8 hex&gt; -pane</c> convention shape throughout, so it still
        /// decomposes as window/door + pane. The counter covers the remaining genuine collision: two
        /// identical apertures stating the identical shade, or a shade changing back to a value a previous,
        /// now-unused element still carries.
        /// </para>
        /// </summary>
        /// <param name="existingNames">Every building element name already in the building, including any created earlier in this pass.</param>
        /// <param name="buildingElementDefinition">The definition the new element will hold.</param>
        /// <param name="name_ApertureConstruction">The SAM <c>ApertureConstruction</c>'s name.</param>
        /// <param name="featureShade">The shade the member states - the identity the plain signature omits.</param>
        public static string ShadedBuildingElementName(IEnumerable<string> existingNames, BuildingElementDefinition buildingElementDefinition, string name_ApertureConstruction, FeatureShade featureShade)
        {
            //The ordinary budget first: a model with a single shade variant keeps the clean names.
            string name = BuildingElementName(existingNames, buildingElementDefinition, name_ApertureConstruction, out string _);
            if (name != null)
            {
                return name;
            }

            if (buildingElementDefinition == null)
            {
                return null;
            }

            string sufix = buildingElementDefinition.AperturePart.Sufix();
            if (string.IsNullOrWhiteSpace(sufix))
            {
                return null;
            }

            string prefix = BuildingElementNamePrefix(buildingElementDefinition.ApertureType);
            string @base = ConstructionNameBase(name_ApertureConstruction);
            string discriminator = Fnv1aHex(BuildingElementSignature(buildingElementDefinition) + "|" + FeatureShadeSignature(featureShade));

            HashSet<string> names = new HashSet<string>((existingNames ?? Enumerable.Empty<string>()).Where(x => x != null));

            string candidate = string.Format("{0}{1}_{2} {3}", prefix, @base, discriminator, sufix);
            int counter = 1;
            while (names.Contains(candidate))
            {
                counter++;
                candidate = string.Format("{0}{1}_{2}_{3} {4}", prefix, @base, discriminator, counter, sufix);
            }

            return candidate;
        }

        /// <summary>
        /// The shade's contribution to a name discriminator: the stored-float bit pattern of every physical
        /// field, the same convention <c>ApertureTypeSignature</c>'s SingleBitsHex uses - identical on every
        /// runtime and build, and matching the precision TBD actually stores. Text (name/description) is
        /// excluded exactly as <see cref="FeatureShadesMatch"/> excludes it.
        /// </summary>
        private static string FeatureShadeSignature(FeatureShade featureShade)
        {
            if (featureShade == null)
            {
                return string.Empty;
            }

            return string.Join("|",
                Bits(featureShade.SurfaceHeight),
                Bits(featureShade.SurfaceWidth),
                Bits(featureShade.LeftFinDepth),
                Bits(featureShade.LeftFinOffset),
                Bits(featureShade.LeftFinTransmittance),
                Bits(featureShade.RightFinDepth),
                Bits(featureShade.RightFinOffset),
                Bits(featureShade.RightFinTransmittance),
                Bits(featureShade.OverhangDepth),
                Bits(featureShade.OverhangOffset),
                Bits(featureShade.OverhangTransmittance));

            static string Bits(double value)
            {
                byte[] bytes = global::System.BitConverter.GetBytes((float)value);
                uint bits = global::System.BitConverter.IsLittleEndian
                    ? (uint)bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24)
                    : (uint)bytes[3] | ((uint)bytes[2] << 8) | ((uint)bytes[1] << 16) | ((uint)bytes[0] << 24);

                return bits.ToString("X8", global::System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
