// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// <b>What one physical aperture part asks for.</b> A single (aperture, part) pairing carried alongside
    /// the reusable <see cref="Tas.ConstructionDefinition"/> and <see cref="Tas.BuildingElementDefinition"/>
    /// that state its content - the COM-free half of what the gbXML canonicalisation pass does, so the
    /// question "how many definitions do these N windows need" can be answered and asserted without an
    /// installed TAS.
    /// <para>
    /// <b>The pairing is physical; the definitions are not.</b> Twenty identical windows produce forty
    /// bindings - one per window per part - and those forty bindings carry just two distinct
    /// <see cref="Tas.BuildingElementDefinition"/>s between them. Nothing here merges an aperture with
    /// another aperture; what is shared is the definition each one points at.
    /// </para>
    /// </summary>
    public sealed class ApertureDefinitionBinding
    {
        /// <param name="apertureGuid">The physical SAM aperture.</param>
        /// <param name="aperturePart">Which half of it - pane or frame.</param>
        /// <param name="constructionDefinition">The construction content it states, or null when it could not be resolved.</param>
        /// <param name="buildingElementDefinition">The element definition it states, or null when it could not be resolved.</param>
        /// <param name="refusal">Why no definition could be resolved, or null.</param>
        public ApertureDefinitionBinding(Guid apertureGuid, AperturePart aperturePart, ConstructionDefinition constructionDefinition, BuildingElementDefinition buildingElementDefinition, string refusal)
        {
            ApertureGuid = apertureGuid;
            AperturePart = aperturePart;
            ConstructionDefinition = constructionDefinition;
            BuildingElementDefinition = buildingElementDefinition;
            Refusal = refusal;
        }

        /// <summary>The physical SAM aperture this binding belongs to.</summary>
        public Guid ApertureGuid { get; }

        /// <summary>Which half of the aperture - pane or frame.</summary>
        public AperturePart AperturePart { get; }

        /// <summary>The reusable construction content, or null.</summary>
        public ConstructionDefinition ConstructionDefinition { get; }

        /// <summary>The reusable element definition, or null.</summary>
        public BuildingElementDefinition BuildingElementDefinition { get; }

        /// <summary>Why nothing could be resolved, or null when it could.</summary>
        public string Refusal { get; }

        /// <summary>Whether this part resolved to a definition that may be shared.</summary>
        public bool Shareable
        {
            get { return BuildingElementDefinition != null && BuildingElementDefinition.Proven; }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} -> {2}", ApertureGuid, AperturePart, BuildingElementDefinition == null ? "(unresolved)" : BuildingElementDefinition.ToString());
        }
    }
}
