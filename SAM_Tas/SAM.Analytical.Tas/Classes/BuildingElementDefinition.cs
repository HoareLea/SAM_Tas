// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What makes two TBD aperture building elements the same element.</b> An immutable, COM-free value
    /// object over everything the export puts on a window's or door's <c>TBD.buildingElement</c>: whether it
    /// is a window or a door, which half of the aperture it is, its <c>BEType</c>, its colour, the
    /// construction it carries, and the ordered set of openings assigned to it.
    /// <para>
    /// <b>A building element is a TYPE, not an instance.</b> The physical windows are the
    /// <c>zoneSurface</c>es, and they stay one per window whatever happens here - two hundred windows keep
    /// two hundred pane surfaces and two hundred frame surfaces. What this class decides is how many
    /// DEFINITIONS those surfaces point at: two, when all two hundred windows state the same glazing,
    /// colour and opening control.
    /// </para>
    /// <para>
    /// <b>Windows and doors never merge</b> - <see cref="ApertureType"/> is a field of its own, not something
    /// inferred from <see cref="BEType"/>, which this export sets from the aperture PART and so cannot tell
    /// a door's pane from a window's. <b>Pane and frame never merge</b> either, for the same reason
    /// <see cref="ConstructionDefinition"/> keeps them apart: the import reads a window's two halves back
    /// from the <c>-pane</c>/<c>-frame</c> pair.
    /// </para>
    /// <para>
    /// <b>No openings is a definition, not a gap.</b> A window that states no <c>OpeningProperties</c> has an
    /// empty <see cref="ApertureTypes"/> list, and that is a perfectly good shared definition: every sealed
    /// window in the model resolves to the one bare element. It is NOT equal to an element that carries an
    /// opening - an empty list and a one-entry list are different lists.
    /// </para>
    /// <para><b>Instances are immutable.</b> A shared definition is never rewritten: on a cache hit the
    /// element is assigned to this aperture's surfaces and nothing whatever is written to it.</para>
    /// </summary>
    public sealed class BuildingElementDefinition : IEquatable<BuildingElementDefinition>
    {
        private readonly ApertureTypeAssignment[] apertureTypes;

        /// <param name="apertureType">Window or door. Its own field, so the two never merge.</param>
        /// <param name="aperturePart">Pane or frame. <see cref="Analytical.AperturePart.Undefined"/> never equals anything.</param>
        /// <param name="bEType">The <c>buildingElement.BEType</c> this export writes.</param>
        /// <param name="colour">The <c>buildingElement.colour</c> this export writes.</param>
        /// <param name="constructionDefinition">The construction the element carries. Null, or unproven, makes the element unproven and so never shareable.</param>
        /// <param name="apertureTypes">The element's openings in child order - empty for a frame, and empty for a pane whose aperture states no opening properties.</param>
        public BuildingElementDefinition(ApertureType apertureType, AperturePart aperturePart, int bEType, uint colour, ConstructionDefinition constructionDefinition, IEnumerable<ApertureTypeAssignment> apertureTypes)
        {
            ApertureType = apertureType;
            AperturePart = aperturePart;
            BEType = bEType;
            Colour = colour;
            ConstructionDefinition = constructionDefinition;
            this.apertureTypes = apertureTypes == null ? new ApertureTypeAssignment[0] : apertureTypes.ToArray();
        }

        /// <summary>Window or door.</summary>
        public ApertureType ApertureType { get; }

        /// <summary>Pane or frame.</summary>
        public AperturePart AperturePart { get; }

        /// <summary>The TAS building-element type.</summary>
        public int BEType { get; }

        /// <summary>The element's colour, as the <c>uint</c> TBD stores.</summary>
        public uint Colour { get; }

        /// <summary>The construction the element carries.</summary>
        public ConstructionDefinition ConstructionDefinition { get; }

        /// <summary>The element's openings in child order. A copy - the stored array is never handed out.</summary>
        public ApertureTypeAssignment[] ApertureTypes
        {
            get { return (ApertureTypeAssignment[])apertureTypes.Clone(); }
        }

        /// <summary>How many openings the element carries. Zero is a valid, shareable definition.</summary>
        public int ApertureTypeCount
        {
            get { return apertureTypes.Length; }
        }

        /// <summary>
        /// Whether everything about this element is known: a real part, a proven construction, and every
        /// opening's control resolved. An unproven element is still created and named, but never shared -
        /// neither offered nor matched.
        /// <para>
        /// <see cref="ApertureType"/> takes no part in this. An aperture that states neither window nor door
        /// is still an aperture the write has always handled, and <see cref="ApertureType.Undefined"/> is a
        /// distinct value here rather than a missing one - so such elements share among themselves and never
        /// merge with a real window. <see cref="AperturePart"/> is different: without a part there is no
        /// <c>-pane</c>/<c>-frame</c> suffix, and so no name and nothing the import could read back.
        /// </para>
        /// </summary>
        public bool Proven
        {
            get
            {
                return AperturePart != AperturePart.Undefined
                    && ConstructionDefinition != null
                    && ConstructionDefinition.Proven
                    && apertureTypes.All(x => x != null && x.Proven);
            }
        }

        public bool Equals(BuildingElementDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (!Proven || !other.Proven)
            {
                return false;
            }

            if (ApertureType != other.ApertureType || AperturePart != other.AperturePart)
            {
                return false;
            }

            if (BEType != other.BEType || Colour != other.Colour)
            {
                return false;
            }

            if (!ConstructionDefinition.Equals(other.ConstructionDefinition))
            {
                return false;
            }

            //Order and length both significant: the openings are the element's children in child order, and
            //a differing count is a differing number of openings.
            return apertureTypes.Length == other.apertureTypes.Length && apertureTypes.SequenceEqual(other.apertureTypes);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BuildingElementDefinition);
        }

        /// <summary>
        /// Consistent with <see cref="Equals(BuildingElementDefinition)"/> for proven definitions, and
        /// derived from the same deterministic signature the naming uses rather than from
        /// <c>string.GetHashCode</c>. Reuse itself never depends on this: the lookup is a full equality scan.
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked((int)Query.Fnv1a(Query.BuildingElementSignature(this) ?? string.Empty));
        }

        public override string ToString()
        {
            return Query.BuildingElementSignature(this) ?? "BuildingElementDefinition";
        }
    }
}
