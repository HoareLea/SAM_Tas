// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What makes two TBD aperture constructions the same construction.</b> An immutable, COM-free value
    /// object over the effective content of a <c>TBD.Construction</c>: its opaque/transparent type, its
    /// ordered layers, the additional heat transfer, the description, and which half of a window it is.
    /// <para>
    /// A <c>TBD.Construction</c> is a building-level REUSABLE DEFINITION, assignable to any number of
    /// building elements - the same relationship a <c>TBD.ApertureType</c> has, one level down. Two hundred
    /// identical windows therefore need two constructions, a pane and a frame, and four hundred assignments;
    /// not four hundred constructions. This class is the equality that decides that, and a NAME takes no
    /// part in it, exactly as it takes no part in <see cref="ApertureTypeDefinition"/>.
    /// </para>
    /// <para>
    /// <b>The previous behaviour on this path was name-only</b>: a construction whose name matched was
    /// adopted whatever it contained. Because the name carried the aperture's own GUID that was harmless in
    /// practice and useless for sharing; once names are derived from the reusable SAM
    /// <c>ApertureConstruction</c> instead, name-only adoption would silently give one window another
    /// window's glazing. Hence: reuse on full content equality, and a deterministic collision-suffixed name
    /// when a name is taken by different content.
    /// </para>
    /// <para>
    /// <b><see cref="AperturePart"/> is part of identity, deliberately.</b> Not because TAS stores it - it
    /// does not - but because the import pairs a window's two constructions by stripping the
    /// <c>-pane</c>/<c>-frame</c> suffix from their names and reading each side's layers
    /// (<c>Convert.ToSAM_ApertureConstruction</c>). A pane and a frame that happened to hold the same layers
    /// would collapse into one construction, and the round trip would come back with one side missing. So
    /// pane and frame are never merged, however identical their content.
    /// </para>
    /// <para>
    /// <b><see cref="Description"/> and <see cref="AdditionalHeatTransfer"/> are identity even though this
    /// export writes neither.</b> They are what tells a construction this export authored apart from one
    /// that was already in the TBD carrying an additional heat transfer or a description of its own - which
    /// is exactly the case where adopting it would change what the model states.
    /// </para>
    /// <para><b>Instances are immutable.</b> A shared definition is never rewritten - see the
    /// mutation-safety rule in the aperture-reuse design notes.</para>
    /// </summary>
    public sealed class ConstructionDefinition : IEquatable<ConstructionDefinition>
    {
        private readonly ConstructionLayerDefinition[] layers;

        /// <param name="aperturePart">Which half of the window this construction is. <see cref="Analytical.AperturePart.Undefined"/> never equals anything, so a construction whose part could not be established is never reused.</param>
        /// <param name="type">The <c>TBD.ConstructionTypes</c> value the construction carries.</param>
        /// <param name="additionalHeatTransfer">As stored on <c>construction.additionalHeatTransfer</c>.</param>
        /// <param name="description">As stored on <c>construction.description</c>. Empty and whitespace normalise to null - a construction with no description reads back as an empty string, and that is the same construction as one that never had one.</param>
        /// <param name="layers">The layers, in TBD order. Order is significant: a construction is its layer sequence.</param>
        public ConstructionDefinition(AperturePart aperturePart, int type, float additionalHeatTransfer, string description, IEnumerable<ConstructionLayerDefinition> layers)
        {
            AperturePart = aperturePart;
            Type = type;
            AdditionalHeatTransfer = ConstructionMaterialDefinition.NormalizeSingle(additionalHeatTransfer);
            Description = string.IsNullOrWhiteSpace(description) ? null : description;
            this.layers = layers == null ? new ConstructionLayerDefinition[0] : layers.ToArray();
        }

        /// <summary>Which half of the window this construction is.</summary>
        public AperturePart AperturePart { get; }

        /// <summary>The <c>TBD.ConstructionTypes</c> value: transparent, or the opaque default.</summary>
        public int Type { get; }

        /// <summary>The additional heat transfer TBD carries.</summary>
        public float AdditionalHeatTransfer { get; }

        /// <summary>The description, normalised so empty and absent are the same thing.</summary>
        public string Description { get; }

        /// <summary>The layers in TBD order. A copy - the stored array is never handed out.</summary>
        public ConstructionLayerDefinition[] Layers
        {
            get { return (ConstructionLayerDefinition[])layers.Clone(); }
        }

        /// <summary>How many layers the construction has.</summary>
        public int LayerCount
        {
            get { return layers.Length; }
        }

        /// <summary>
        /// Whether every layer resolved to material content. A construction with an unresolved layer is
        /// unknown content: it is still created and named, but it never takes part in reuse - neither as a
        /// candidate nor as a match.
        /// </summary>
        public bool Proven
        {
            get { return AperturePart != AperturePart.Undefined && layers.All(x => x != null && x.Material != null); }
        }

        public bool Equals(ConstructionDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            //Unproven content is never equal to anything, including itself by value: an unresolved layer
            //means this definition does not know what TBD holds, and reuse requires knowing.
            if (!Proven || !other.Proven)
            {
                return false;
            }

            if (AperturePart != other.AperturePart || Type != other.Type)
            {
                return false;
            }

            if (!AdditionalHeatTransfer.Equals(other.AdditionalHeatTransfer))
            {
                return false;
            }

            if (!string.Equals(Description, other.Description, StringComparison.Ordinal))
            {
                return false;
            }

            return layers.Length == other.layers.Length && layers.SequenceEqual(other.layers);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConstructionDefinition);
        }

        /// <summary>
        /// Consistent with <see cref="Equals(ConstructionDefinition)"/> for proven definitions, and derived
        /// from the same deterministic signature the naming uses rather than from <c>string.GetHashCode</c>.
        /// Reuse itself never depends on this: the lookup is a full equality scan.
        /// </summary>
        public override int GetHashCode()
        {
            return unchecked((int)Query.Fnv1a(Query.ConstructionSignature(this) ?? string.Empty));
        }

        public override string ToString()
        {
            return Query.ConstructionSignature(this) ?? "ConstructionDefinition";
        }
    }
}
