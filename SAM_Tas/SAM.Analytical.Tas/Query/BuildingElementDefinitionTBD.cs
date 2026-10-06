// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The seed reader: everything readable off an aperture building element that was already in the
        /// TBD, turned into a value.</b>
        /// <para>
        /// This reads and does nothing else. Whether the element may be shared, and what it is a definition
        /// of, is decided by the COM-free
        /// <see cref="BuildingElementDefinition(BuildingElementSeed, out string)"/>, which is where the gates
        /// and their reasoning live.
        /// </para>
        /// </summary>
        /// <param name="buildingElement">The pre-existing TBD building element.</param>
        /// <param name="cache">The open document's reuse cache, used to read the element's aperture-control assignments once.</param>
        public static BuildingElementSeed BuildingElementSeed(this TBD.buildingElement buildingElement, BuildingReuseCache cache)
        {
            if (buildingElement == null)
            {
                return null;
            }

            string name = buildingElement.name;

            //Read no further than the name until the name says this could be a candidate at all: most of a
            //model's elements are panels, and reading a panel's construction and openings would be COM
            //traffic spent on an answer already known.
            if (!TryDecomposeBuildingElementName(name, out string _, out Analytical.ApertureType _, out Analytical.AperturePart _))
            {
                return new BuildingElementSeed(name, 0, null, false, false, 0, 0, 0, 0, 0, null, null);
            }

            return new BuildingElementSeed(
                name,
                buildingElement.ghost,
                buildingElement.description,
                buildingElement.GetFeatureShade(1) != null,
                buildingElement.GetSubstituteElement(1) != null,
                buildingElement.ground,
                buildingElement.markDelete,
                buildingElement.width,
                buildingElement.BEType,
                buildingElement.colour,
                ConstructionDefinition(buildingElement.GetConstruction(), out string _),
                cache == null ? null : cache.ExistingAssignments(buildingElement));
        }

        /// <summary>
        /// The seed read and the gate in one call, for the cache's classification pass.
        /// </summary>
        /// <param name="buildingElement">The pre-existing TBD building element.</param>
        /// <param name="cache">The open document's reuse cache.</param>
        /// <param name="refusal">Why it may not be shared, or null when the definition is usable.</param>
        public static BuildingElementDefinition BuildingElementDefinition(this TBD.buildingElement buildingElement, BuildingReuseCache cache, out string refusal)
        {
            refusal = null;

            if (buildingElement == null)
            {
                refusal = "No TBD building element to read.";
                return null;
            }

            return BuildingElementDefinition(BuildingElementSeed(buildingElement, cache), out refusal);
        }
    }
}
