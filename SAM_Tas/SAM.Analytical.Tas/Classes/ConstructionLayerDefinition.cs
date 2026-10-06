// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>One layer of a TBD construction: the material, and the two widths TBD stores for it.</b> An
    /// immutable, COM-free value object.
    /// <para>
    /// A TBD construction keeps a layer's thickness in TWO places - <c>construction.materialWidth[i]</c> and
    /// the layer material's own <c>material.width</c> - and the export writes the stated layer thickness to
    /// both. Both are carried here, and both take part in equality, because "the widths as actually written
    /// to TBD" is what the simulation reads: a seeded construction whose material width was left at the
    /// library's default thickness while its <c>materialWidth</c> carries the real one is NOT the same layer
    /// as one where the two agree, and must not be adopted as though it were.
    /// </para>
    /// <para><b>Instances are immutable.</b> A shared definition is never rewritten.</para>
    /// </summary>
    public sealed class ConstructionLayerDefinition : IEquatable<ConstructionLayerDefinition>
    {
        /// <param name="material">The layer's material content. Null means the layer could not be resolved, which makes the whole construction unproven.</param>
        /// <param name="width">As written to <c>construction.materialWidth[i]</c>.</param>
        /// <param name="materialWidth">As written to <c>material.width</c>.</param>
        public ConstructionLayerDefinition(ConstructionMaterialDefinition material, float width, float materialWidth)
        {
            Material = material;
            Width = ConstructionMaterialDefinition.NormalizeSingle(width);
            MaterialWidth = ConstructionMaterialDefinition.NormalizeSingle(materialWidth);
        }

        /// <summary>The layer's material content.</summary>
        public ConstructionMaterialDefinition Material { get; }

        /// <summary>The thickness held by <c>construction.materialWidth[i]</c>.</summary>
        public float Width { get; }

        /// <summary>The thickness held by the layer material's own <c>material.width</c>.</summary>
        public float MaterialWidth { get; }

        public bool Equals(ConstructionLayerDefinition other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            if (!Width.Equals(other.Width) || !MaterialWidth.Equals(other.MaterialWidth))
            {
                return false;
            }

            //Two layers whose material could not be resolved are not thereby "the same layer" - an
            //unresolved layer is unknown content, and unknown content is never proven equal.
            if (Material == null || other.Material == null)
            {
                return false;
            }

            return Material.Equals(other.Material);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ConstructionLayerDefinition);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Query.Fnv1a(Query.ConstructionLayerSignature(this) ?? string.Empty));
        }

        public override string ToString()
        {
            return Query.ConstructionLayerSignature(this) ?? "ConstructionLayerDefinition";
        }
    }
}
