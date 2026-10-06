// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The prefix TAS gives a window building element in a native <c>.tbd</c>. The export has always
        /// mirrored it; it is named here so the name and its decomposition cannot drift apart.
        /// </summary>
        public const string BuildingElementNamePrefix_Windows = "Windows: ";

        /// <summary>The same, for a door.</summary>
        public const string BuildingElementNamePrefix_Doors = "Doors: ";

        //^Windows: |Doors: <base>[_<collision hash>] -pane|-frame$
        private static readonly Regex regex_BuildingElementName = new Regex(
            @"^(?<prefix>Windows: |Doors: )(?<base>.+?)(?:_(?<hash>[0-9A-F]{8}))? (?<sufix>-pane|-frame)$",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// The <c>"Windows: "</c> / <c>"Doors: "</c> prefix a building element of this aperture type carries.
        /// </summary>
        public static string BuildingElementNamePrefix(this Analytical.ApertureType apertureType)
        {
            return apertureType == Analytical.ApertureType.Door ? BuildingElementNamePrefix_Doors : BuildingElementNamePrefix_Windows;
        }

        /// <summary>
        /// The name to create a NEW aperture building element under, given the names already in the building.
        /// <para>
        /// <b>No part of this name comes from a physical aperture</b> - the base is the SAM
        /// <c>ApertureConstruction</c>'s own name, where the previous naming used
        /// <c>aperture.UniqueName()</c> and so embedded the aperture's GUID. The result reads as what it now
        /// is, a definition rather than an instance:
        /// </para>
        /// <code>
        /// Windows: SIM_EXT_GLZ -pane
        /// Windows: SIM_EXT_GLZ -frame
        /// </code>
        /// <para>
        /// The <c>Windows: </c>/<c>Doors: </c> prefix is kept exactly as before, and the part suffix stays
        /// TERMINAL for the same reason it does on a construction name - a collision discriminator is
        /// appended to the base, <c>Windows: SIM_EXT_GLZ_1F3A0C21 -pane</c>.
        /// </para>
        /// <para>
        /// <b>Reached only after a DEFINITION search has failed</b>, so any existing element of the preferred
        /// name necessarily has a different definition, or one this export may not adopt, and is never
        /// written to.
        /// </para>
        /// </summary>
        /// <param name="existingNames">Every building element name already in the building, including any created earlier in this export.</param>
        /// <param name="buildingElementDefinition">The definition the new element will hold.</param>
        /// <param name="name_ApertureConstruction">The SAM <c>ApertureConstruction</c>'s name.</param>
        /// <param name="refusal">Why no name could be chosen, or null on success.</param>
        public static string BuildingElementName(IEnumerable<string> existingNames, BuildingElementDefinition buildingElementDefinition, string name_ApertureConstruction, out string refusal)
        {
            refusal = null;

            if (buildingElementDefinition == null)
            {
                refusal = "No building element definition was supplied, so no building element name was derived and nothing was created.";
                return null;
            }

            string sufix = buildingElementDefinition.AperturePart.Sufix();
            if (string.IsNullOrWhiteSpace(sufix))
            {
                refusal = string.Format("An aperture building element is either a pane or a frame; '{0}' is neither, so no building element name was derived and nothing was created.", buildingElementDefinition.AperturePart);
                return null;
            }

            HashSet<string> names = new HashSet<string>(existingNames == null ? Enumerable.Empty<string>() : existingNames.Where(x => x != null));

            string prefix = BuildingElementNamePrefix(buildingElementDefinition.ApertureType);
            string @base = ConstructionNameBase(name_ApertureConstruction);

            string preferred = string.Format("{0}{1} {2}", prefix, @base, sufix);
            if (!names.Contains(preferred))
            {
                return preferred;
            }

            string qualified = string.Format("{0}{1}_{2} {3}", prefix, @base, BuildingElementSignatureHash(buildingElementDefinition), sufix);
            if (!names.Contains(qualified))
            {
                return qualified;
            }

            refusal = string.Format("TBD building element '{0}' already exists with a different definition, and so does the signature-qualified alternative '{1}'. Rather than guess a third name or overwrite a building element this export did not author, nothing was written.", preferred, qualified);
            return null;
        }

        /// <summary>
        /// Whether <paramref name="name"/> has the shape
        /// <see cref="BuildingElementName(IEnumerable{string}, BuildingElementDefinition, string, out string)"/>
        /// generates, and if so which aperture type and which half of it the name states.
        /// <para>
        /// This is a NAME test and proves only that the name follows this export's convention. It never
        /// decides reuse - reuse is full definitional equality. What it does decide is whether a pre-existing
        /// element is a CANDIDATE at all: window-or-door and pane-or-frame are not stored anywhere on a TBD
        /// building element that this export could read back (<c>BEType</c> is set from the part, so it
        /// cannot tell a door's pane from a window's), so an element whose name does not carry the convention
        /// has an unknown definition and is left alone.
        /// </para>
        /// </summary>
        public static bool TryDecomposeBuildingElementName(string name, out string @base, out Analytical.ApertureType apertureType, out Analytical.AperturePart aperturePart)
        {
            @base = null;
            apertureType = Analytical.ApertureType.Undefined;
            aperturePart = Analytical.AperturePart.Undefined;

            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            Match match = regex_BuildingElementName.Match(name);
            if (!match.Success)
            {
                return false;
            }

            @base = match.Groups["base"].Value;

            apertureType = string.Equals(match.Groups["prefix"].Value, BuildingElementNamePrefix_Doors, global::System.StringComparison.Ordinal)
                ? Analytical.ApertureType.Door
                : Analytical.ApertureType.Window;

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
