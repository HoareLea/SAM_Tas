// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The gate: may this export share a building element that was already in the TBD, and if so what
        /// is it a definition of?</b> A pure function of what was read off the element - no COM - so the
        /// decision is testable without an installed TAS.
        /// <para>
        /// An element this export did not author is a foreign object until proven otherwise, and every gate
        /// below refuses rather than infers. The asymmetry is the point: refusing costs one extra building
        /// element, whereas adopting a foreign element would silently give a window that element's
        /// construction, colour, shading and openings - and, because a shared definition is never written to,
        /// there would be no second chance to correct it.
        /// </para>
        /// <para><b>The gates</b>, each of which refuses:</para>
        /// <list type="bullet">
        /// <item>a name that does not carry this export's <c>Windows: …/-pane</c> convention - window-or-door
        /// and pane-or-frame are not stored anywhere readable on a TBD element (<c>BEType</c> is written from
        /// the PART, so it cannot tell a door's pane from a window's), so an element that does not name them
        /// has an unknowable definition;</item>
        /// <item>a non-default <c>ghost</c> - the export leaves an aperture element solid, and a ghost element
        /// is a different thing entirely;</item>
        /// <item>a non-empty <c>description</c> - the export writes none, so one that has a description is
        /// carrying somebody else's meaning;</item>
        /// <item>an assigned feature shade, or an assigned substitute element - controls the export never
        /// assigns, and whose effect a shared element would spread to every window using it;</item>
        /// <item>a non-default <c>ground</c>, <c>markDelete</c> or <c>width</c> - readable fields the export
        /// does not write, so a non-default value means the element was configured by something else;</item>
        /// <item>a construction that could not be read, or whose own naming cannot establish which half of a
        /// window it is;</item>
        /// <item>an opening this export cannot classify: an aperture type whose control may not be reused, or
        /// whose name does not carry the Stage 1 convention that states its occurrence;</item>
        /// <item>any opening at all on a FRAME - only a pane write reaches <c>SetApertureTypes</c>, so a frame
        /// carrying an opening is not one of ours.</item>
        /// </list>
        /// <para>
        /// Passing every gate does not make the element shareable; it makes it a CANDIDATE, whose definition
        /// is then compared field by field with the one an aperture asks for.
        /// </para>
        /// </summary>
        /// <param name="buildingElementSeed">What was read off the element.</param>
        /// <param name="refusal">Why it may not be shared, or null when the definition is usable.</param>
        /// <returns>The element's definition, or null when <paramref name="refusal"/> is set.</returns>
        public static BuildingElementDefinition BuildingElementDefinition(this BuildingElementSeed buildingElementSeed, out string refusal)
        {
            refusal = null;

            if (buildingElementSeed == null)
            {
                refusal = "No building element seed to classify.";
                return null;
            }

            string name = buildingElementSeed.Name;

            if (!TryDecomposeBuildingElementName(name, out string _, out Analytical.ApertureType apertureType, out Analytical.AperturePart aperturePart))
            {
                refusal = string.Format("TBD building element '{0}' does not carry this export's aperture naming, so whether it is a window or a door and which half of one it is cannot be established; it was not shared.", name);
                return null;
            }

            if (buildingElementSeed.Ghost != 0)
            {
                refusal = string.Format("TBD building element '{0}' is a ghost element, which this export never writes; it was not shared.", name);
                return null;
            }

            if (!string.IsNullOrWhiteSpace(buildingElementSeed.Description))
            {
                refusal = string.Format("TBD building element '{0}' carries a description this export did not write, so it states something this export cannot account for; it was not shared.", name);
                return null;
            }

            if (buildingElementSeed.FeatureShade)
            {
                refusal = string.Format("TBD building element '{0}' has an assigned feature shade. Sharing it would apply that shade to every aperture using the element, so it was not shared.", name);
                return null;
            }

            if (buildingElementSeed.SubstituteElement)
            {
                refusal = string.Format("TBD building element '{0}' has an assigned substitute element, which this export never assigns; it was not shared.", name);
                return null;
            }

            if (buildingElementSeed.Ground != 0 || buildingElementSeed.MarkDelete != 0 || buildingElementSeed.Width != 0)
            {
                refusal = string.Format("TBD building element '{0}' carries a ground, mark-delete or width setting this export does not write, so it was configured by something else; it was not shared.", name);
                return null;
            }

            ConstructionDefinition constructionDefinition = buildingElementSeed.ConstructionDefinition;
            if (constructionDefinition == null)
            {
                refusal = string.Format("TBD building element '{0}' carries a construction this export could not read or could not classify as a pane or a frame, so it was not shared.", name);
                return null;
            }

            KeyValuePair<string, ApertureTypeDefinition>[] assignments = buildingElementSeed.ApertureTypeAssignments;

            if (assignments.Length != 0 && aperturePart != Analytical.AperturePart.Pane)
            {
                refusal = string.Format("TBD building element '{0}' is a frame carrying {1} aperture control(s). This export only writes controls onto a pane, so it was not shared.", name, assignments.Length);
                return null;
            }

            List<ApertureTypeAssignment> apertureTypeAssignments = new List<ApertureTypeAssignment>();
            foreach (KeyValuePair<string, ApertureTypeDefinition> assignment in assignments)
            {
                if (assignment.Value == null)
                {
                    refusal = string.Format("TBD building element '{0}' carries aperture type '{1}', whose opening control this export may not reuse, so the element was not shared either.", name, assignment.Key);
                    return null;
                }

                //The occurrence an aperture type stands for lives in its name, by the Stage 1 convention. A
                //type outside that convention has an unknown occurrence, and multiplicity has to be exact.
                if (!TryDecomposeApertureTypeName(assignment.Key, out string _, out int ordinal))
                {
                    refusal = string.Format("TBD building element '{0}' carries aperture type '{1}', which is not named by this export's convention, so which occurrence it stands for is unknown; the element was not shared.", name, assignment.Key);
                    return null;
                }

                apertureTypeAssignments.Add(new ApertureTypeAssignment(assignment.Value, ordinal));
            }

            return new BuildingElementDefinition(
                apertureType,
                aperturePart,
                buildingElementSeed.BEType,
                buildingElementSeed.Colour,
                constructionDefinition,
                apertureTypeAssignments);
        }

        /// <summary>
        /// <b>The gate for a construction that was already in the TBD</b>, as a pure function of what was read
        /// off it - no COM, so the decision is testable without an installed TAS.
        /// <para>
        /// <b>The part comes from the name, and only from this export's own convention</b> - TAS stores nothing
        /// on a construction that says "pane" or "frame". A construction whose name does not carry the
        /// convention is refused rather than guessed at. Adopting a construction whose CONTENT matches is safe
        /// whoever named it, since the simulation reads content; but merging a frame into a pane would break
        /// the aperture import, which reads a window's two halves back from the <c>-pane</c>/<c>-frame</c>
        /// pair, so the tag has to be established rather than assumed.
        /// </para>
        /// <para>
        /// <b>Name and content are separate questions.</b> This establishes what a construction HOLDS; whether
        /// that is what an aperture wants is decided afterwards by full equality. A construction whose name
        /// matches a wanted one but whose layers differ therefore resolves to a different definition and is
        /// never adopted - the unsafe by-name behaviour this replaces.
        /// </para>
        /// </summary>
        /// <param name="name">The construction's name.</param>
        /// <param name="type">As read from <c>construction.type</c>.</param>
        /// <param name="additionalHeatTransfer">As read from <c>construction.additionalHeatTransfer</c>.</param>
        /// <param name="description">As read from <c>construction.description</c>.</param>
        /// <param name="constructionLayerDefinitions">The layers in TBD order, each already read.</param>
        /// <param name="refusal">Why it may not be reused, or null when the definition is usable.</param>
        public static ConstructionDefinition ConstructionDefinition(string name, int type, float additionalHeatTransfer, string description, IEnumerable<ConstructionLayerDefinition> constructionLayerDefinitions, out string refusal)
        {
            refusal = null;

            if (!TryDecomposeConstructionName(name, out string _, out Analytical.AperturePart aperturePart))
            {
                refusal = string.Format("TBD construction '{0}' does not carry the '-pane'/'-frame' naming this export and the aperture import both use, so which half of a window it is cannot be established and it was not reused.", name);
                return null;
            }

            if (constructionLayerDefinitions == null)
            {
                refusal = string.Format("TBD construction '{0}' reported no layers, so its content could not be read and it was not reused.", name);
                return null;
            }

            int index = 0;
            foreach (ConstructionLayerDefinition constructionLayerDefinition in constructionLayerDefinitions)
            {
                index++;

                if (constructionLayerDefinition == null || constructionLayerDefinition.Material == null)
                {
                    refusal = string.Format("TBD construction '{0}' has a layer at position {1} whose material could not be read, so it was not reused.", name, index);
                    return null;
                }
            }

            return new ConstructionDefinition(aperturePart, type, additionalHeatTransfer, description, constructionLayerDefinitions);
        }
    }
}
