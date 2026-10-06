// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// <b>The seed reader: what an aperture construction ALREADY in the TBD actually holds</b>, or the
        /// reason it may not be reused.
        /// <para>
        /// This reads and does nothing else. Every decision belongs to the COM-free
        /// <see cref="ConstructionDefinition(string, int, float, string, IEnumerable{ConstructionLayerDefinition}, out string)"/>,
        /// which is where the gates and their reasoning live.
        /// </para>
        /// </summary>
        /// <param name="construction">The pre-existing TBD construction.</param>
        /// <param name="refusal">Why it may not be reused, or null when the definition is usable.</param>
        /// <returns>The construction's content, or null when <paramref name="refusal"/> is set.</returns>
        public static ConstructionDefinition ConstructionDefinition(this TBD.Construction construction, out string refusal)
        {
            refusal = null;

            if (construction == null)
            {
                refusal = "No TBD construction to read.";
                return null;
            }

            string name = construction.name;

            //Read no further than the name until the name says this could be a candidate at all: a panel's
            //construction never is one, and its layers would be COM traffic spent on an answer already known.
            if (!TryDecomposeConstructionName(name, out string _, out AperturePart _))
            {
                return ConstructionDefinition(name, 0, 0, null, null, out refusal);
            }

            List<TBD.material> materials = construction.Materials();
            if (materials == null)
            {
                refusal = string.Format("TBD construction '{0}' did not report its materials, so its content could not be read and it was not reused.", name);
                return null;
            }

            List<ConstructionLayerDefinition> constructionLayerDefinitions = new List<ConstructionLayerDefinition>();
            for (int i = 0; i < materials.Count; i++)
            {
                TBD.material material = materials[i];

                //materialWidth is 1-based, as everywhere else in the TBD interop. A null material yields a
                //layer with no material content, which the gate turns into a refusal.
                constructionLayerDefinitions.Add(new ConstructionLayerDefinition(
                    ConstructionMaterialDefinitionTBD(material),
                    construction.materialWidth[i + 1],
                    material == null ? 0 : material.width));
            }

            return ConstructionDefinition(name, (int)construction.type, construction.additionalHeatTransfer, construction.description, constructionLayerDefinitions, out refusal);
        }

        /// <summary>
        /// The seed reader's material half: every field
        /// <see cref="ConstructionMaterialDefinition(Core.IMaterial)"/> predicts, read back off the TBD
        /// material that is really there. <c>width</c> is read by the caller instead, because it belongs to
        /// the layer.
        /// </summary>
        public static ConstructionMaterialDefinition ConstructionMaterialDefinitionTBD(this TBD.material material)
        {
            if (material == null)
            {
                return null;
            }

            return new ConstructionMaterialDefinition(
                name: material.name,
                type: material.type,
                description: material.description,
                conductivity: material.conductivity,
                specificHeat: material.specificHeat,
                density: material.density,
                vapourDiffusionFactor: material.vapourDiffusionFactor,
                externalSolarReflectance: material.externalSolarReflectance,
                internalSolarReflectance: material.internalSolarReflectance,
                externalLightReflectance: material.externalLightReflectance,
                internalLightReflectance: material.internalLightReflectance,
                externalEmissivity: material.externalEmissivity,
                internalEmissivity: material.internalEmissivity,
                solarTransmittance: material.solarTransmittance,
                lightTransmittance: material.lightTransmittance,
                dynamicViscosity: material.dynamicViscosity,
                convectionCoefficient: material.convectionCoefficient,
                isBlind: material.isBlind);
        }
    }
}
