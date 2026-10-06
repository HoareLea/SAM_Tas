// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>Everything readable off a pre-existing aperture <c>TBD.buildingElement</c>, read out of COM and
    /// into a value.</b>
    /// <para>
    /// The point of the split is that the DECISION - may this export share this element, and if so what is it
    /// a definition of - is then a pure function of these fields
    /// (<see cref="Query.BuildingElementDefinition(BuildingElementSeed, out string)"/>) and can be tested
    /// without an installed TAS. The COM overload that fills this in does nothing but read.
    /// </para>
    /// <para>
    /// Every field the TBD element exposes is here, whether or not the export writes it: a field left out
    /// would be a field silently excused from the gates, and the rule is that anything unclassified refuses
    /// rather than passes.
    /// </para>
    /// </summary>
    public sealed class BuildingElementSeed
    {
        private readonly KeyValuePair<string, ApertureTypeDefinition>[] apertureTypeAssignments;

        /// <param name="name">The element's name.</param>
        /// <param name="ghost">As read from <c>buildingElement.ghost</c>.</param>
        /// <param name="description">As read from <c>buildingElement.description</c>.</param>
        /// <param name="featureShade">Whether <c>GetFeatureShade(1)</c> returned anything.</param>
        /// <param name="substituteElement">Whether <c>GetSubstituteElement(1)</c> returned anything.</param>
        /// <param name="ground">As read from <c>buildingElement.ground</c>.</param>
        /// <param name="markDelete">As read from <c>buildingElement.markDelete</c>.</param>
        /// <param name="width">As read from <c>buildingElement.width</c>.</param>
        /// <param name="bEType">As read from <c>buildingElement.BEType</c>.</param>
        /// <param name="colour">As read from <c>buildingElement.colour</c>.</param>
        /// <param name="constructionDefinition">What the element's construction holds, or null when it could not be read.</param>
        /// <param name="apertureTypeAssignments">The element's aperture types in element order, each as its name paired with its definition - null where the control may not be reused.</param>
        public BuildingElementSeed(
            string name,
            int ghost,
            string description,
            bool featureShade,
            bool substituteElement,
            int ground,
            int markDelete,
            float width,
            int bEType,
            uint colour,
            ConstructionDefinition constructionDefinition,
            IEnumerable<KeyValuePair<string, ApertureTypeDefinition>> apertureTypeAssignments)
        {
            Name = name;
            Ghost = ghost;
            Description = description;
            FeatureShade = featureShade;
            SubstituteElement = substituteElement;
            Ground = ground;
            MarkDelete = markDelete;
            Width = width;
            BEType = bEType;
            Colour = colour;
            ConstructionDefinition = constructionDefinition;
            this.apertureTypeAssignments = apertureTypeAssignments == null
                ? new KeyValuePair<string, ApertureTypeDefinition>[0]
                : apertureTypeAssignments.ToArray();
        }

        /// <summary>The element's name.</summary>
        public string Name { get; }

        /// <summary>Whether TAS treats the element as a ghost.</summary>
        public int Ghost { get; }

        /// <summary>The element's description.</summary>
        public string Description { get; }

        /// <summary>Whether a feature shade is assigned.</summary>
        public bool FeatureShade { get; }

        /// <summary>Whether a substitute element is assigned.</summary>
        public bool SubstituteElement { get; }

        /// <summary>The element's ground flag.</summary>
        public int Ground { get; }

        /// <summary>The element's mark-delete flag.</summary>
        public int MarkDelete { get; }

        /// <summary>The element's width.</summary>
        public float Width { get; }

        /// <summary>The TAS building-element type.</summary>
        public int BEType { get; }

        /// <summary>The element's colour.</summary>
        public uint Colour { get; }

        /// <summary>What the element's construction holds.</summary>
        public ConstructionDefinition ConstructionDefinition { get; }

        /// <summary>The element's aperture types in element order. A copy - the stored array is never handed out.</summary>
        public KeyValuePair<string, ApertureTypeDefinition>[] ApertureTypeAssignments
        {
            get { return (KeyValuePair<string, ApertureTypeDefinition>[])apertureTypeAssignments.Clone(); }
        }
    }
}
