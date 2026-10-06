// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The TBD aperture building element one half of one SAM aperture asks for, resolved WITHOUT touching
        /// COM - so an element can be identified, compared and named before a single TBD object is read or
        /// created.
        /// <para>
        /// Every field is the value the write will put on the element: the <c>Windows: </c>/<c>Doors: </c>
        /// distinction, the <c>BEType</c> the aperture PART implies (which is what this export has always
        /// written - a door's pane carries the glazing type, so <c>BEType</c> alone cannot tell a door from a
        /// window and <see cref="BuildingElementDefinition.ApertureType"/> is kept as its own field), the
        /// colour <c>Modify.SetColor</c> resolves, the construction, and the openings.
        /// </para>
        /// <para>
        /// <b>Only a pane carries openings.</b> A frame's list is empty, exactly as the write only reaches
        /// <c>SetApertureTypes</c> for a pane.
        /// </para>
        /// <para>
        /// <b>A child whose control cannot be resolved is omitted, not carried as a gap.</b> Such a child's
        /// write is refused, so it puts nothing on the element - and the element the write produces is
        /// therefore identical to one whose model never stated that child. Its siblings keep the ordinals
        /// <see cref="ApertureTypeOrdinals(IEnumerable{ApertureTypeDefinition})"/> gives them, so a refused
        /// child can neither claim nor displace an occurrence.
        /// </para>
        /// </summary>
        /// <param name="aperture">The SAM aperture.</param>
        /// <param name="aperturePart">Which half of it.</param>
        /// <param name="constructionDefinition">The construction the element will carry, as resolved by <see cref="ConstructionDefinition(ApertureConstruction, Analytical.AperturePart, Core.MaterialLibrary, out string)"/>.</param>
        /// <param name="dayTypeNames">The day types every control this export writes applies on - the open document's <c>BuildingReuseCache.DayTypeNames</c>.</param>
        /// <param name="refusal">Why no definition could be resolved, or null on success.</param>
        public static BuildingElementDefinition BuildingElementDefinition(this Aperture aperture, Analytical.AperturePart aperturePart, ConstructionDefinition constructionDefinition, IEnumerable<string> dayTypeNames, out string refusal)
        {
            refusal = null;

            if (aperture == null)
            {
                refusal = "No aperture to resolve a TBD building element from.";
                return null;
            }

            if (aperturePart != Analytical.AperturePart.Pane && aperturePart != Analytical.AperturePart.Frame)
            {
                refusal = string.Format("An aperture building element is either a pane or a frame; '{0}' is neither.", aperturePart);
                return null;
            }

            //An aperture that states NEITHER window nor door is carried as Undefined rather than refused.
            //The write has always handled it - the "Windows: " prefix covers everything that is not a door,
            //and the BEType comes from the part - so refusing here would take a building element away from an
            //aperture that used to get one. Undefined stays a distinct value, so it never merges with a real
            //window; the two are simply both shareable among their own kind.
            Analytical.ApertureType apertureType = aperture.ApertureType;

            //The very colour Modify.SetColor writes. It is only skipped when the aperture or the part is
            //absent, and both have already been checked, so an absent colour here means something this
            //cannot predict and the definition is refused rather than guessed.
            System.Drawing.Color? color = Color(aperture, aperturePart);
            if (color == null || !color.HasValue)
            {
                refusal = "The aperture resolved no colour, so what the building element would carry could not be established.";
                return null;
            }

            return new BuildingElementDefinition(
                apertureType,
                aperturePart,
                BEType(aperturePart),
                Core.Convert.ToUint(color.Value),
                constructionDefinition,
                ApertureTypeAssignments(aperture, aperturePart, dayTypeNames));
        }

        /// <summary>
        /// The openings a building element for this half of this aperture will carry, in child order: each
        /// resolved control paired with the occurrence of that control among the aperture's children.
        /// <para>
        /// Resolved by exactly the route <c>Modify.SetApertureTypes</c> takes - the COM-free
        /// <see cref="ApertureTypeDefinition(ISingleOpeningProperties, IEnumerable{string}, out string)"/>
        /// per child, and <see cref="ApertureTypeOrdinals(IEnumerable{ApertureTypeDefinition})"/> over the
        /// whole sibling set - so the list is what the write will really produce and not a second opinion
        /// about it.
        /// </para>
        /// <para>
        /// Empty for a frame, and empty for a pane whose aperture states no opening properties. An empty list
        /// is a definition in its own right: every sealed window in a model resolves to the one bare element.
        /// </para>
        /// </summary>
        public static List<ApertureTypeAssignment> ApertureTypeAssignments(this Aperture aperture, Analytical.AperturePart aperturePart, IEnumerable<string> dayTypeNames)
        {
            List<ApertureTypeAssignment> result = new List<ApertureTypeAssignment>();

            //A frame never receives an aperture control - only the pane write reaches SetApertureTypes.
            if (aperture == null || aperturePart != Analytical.AperturePart.Pane)
            {
                return result;
            }

            if (!aperture.TryGetValue(Analytical.ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
            {
                return result;
            }

            List<ISingleOpeningProperties> singleOpeningProperties = null;
            if (openingProperties is ISingleOpeningProperties)
            {
                singleOpeningProperties = new List<ISingleOpeningProperties> { (ISingleOpeningProperties)openingProperties };
            }
            else if (openingProperties is MultipleOpeningProperties)
            {
                singleOpeningProperties = ((MultipleOpeningProperties)openingProperties).SingleOpeningProperties;
            }

            if (singleOpeningProperties == null)
            {
                return result;
            }

            List<ApertureTypeDefinition> apertureTypeDefinitions = new List<ApertureTypeDefinition>();
            foreach (ISingleOpeningProperties single in singleOpeningProperties)
            {
                apertureTypeDefinitions.Add(single.ApertureTypeDefinition(dayTypeNames, out string _));
            }

            List<int> ordinals = ApertureTypeOrdinals(apertureTypeDefinitions);

            for (int i = 0; i < apertureTypeDefinitions.Count; i++)
            {
                ApertureTypeDefinition apertureTypeDefinition = apertureTypeDefinitions[i];

                //A child whose control could not be resolved puts nothing on the element, so it contributes
                //nothing here either - see the remarks.
                if (apertureTypeDefinition == null)
                {
                    continue;
                }

                result.Add(new ApertureTypeAssignment(apertureTypeDefinition, i < ordinals.Count ? ordinals[i] : 1));
            }

            return result;
        }
    }
}
