// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>One opening on a building element: the control it states, and which occurrence of that control it
    /// is.</b> An immutable, COM-free value object - the <c>(definition, ordinal)</c> pair Stage 1 already
    /// uses as the aperture-type reuse key, lifted into a value so that a building element's whole opening
    /// set can be compared as one.
    /// <para>
    /// <b>The ordinal is what keeps multiplicity exact.</b> TAS holds one entry per aperture type on an
    /// element, so a window with two identical openings needs two distinct types - occurrence 1 and
    /// occurrence 2 of the same control. A definition list without ordinals could not tell that window
    /// apart from a one-opening window carrying the ordinal-2 type on its own, and sharing a building
    /// element between them would silently change how much either ventilates.
    /// </para>
    /// </summary>
    public sealed class ApertureTypeAssignment : IEquatable<ApertureTypeAssignment>
    {
        /// <param name="apertureTypeDefinition">The control this opening states, or null when it could not be resolved - which makes the element unproven and therefore never shareable.</param>
        /// <param name="ordinal">The 1-based occurrence of that control among the element's openings.</param>
        public ApertureTypeAssignment(ApertureTypeDefinition apertureTypeDefinition, int ordinal)
        {
            ApertureTypeDefinition = apertureTypeDefinition;
            Ordinal = ordinal;
        }

        /// <summary>The control this opening states.</summary>
        public ApertureTypeDefinition ApertureTypeDefinition { get; }

        /// <summary>The 1-based occurrence of that control among the element's openings.</summary>
        public int Ordinal { get; }

        /// <summary>Whether the control behind this opening is known.</summary>
        public bool Proven
        {
            get { return ApertureTypeDefinition != null && Ordinal >= 1; }
        }

        public bool Equals(ApertureTypeAssignment other)
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

            return Ordinal == other.Ordinal && ApertureTypeDefinition.Equals(other.ApertureTypeDefinition);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ApertureTypeAssignment);
        }

        public override int GetHashCode()
        {
            return unchecked((int)Query.Fnv1a(Query.ApertureTypeAssignmentSignature(this) ?? string.Empty));
        }

        public override string ToString()
        {
            return Query.ApertureTypeAssignmentSignature(this) ?? "ApertureTypeAssignment";
        }
    }
}
