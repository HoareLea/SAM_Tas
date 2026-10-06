// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        /// <summary>
        /// The TBD construction one half of a SAM <c>ApertureConstruction</c> asks for, resolved WITHOUT
        /// touching COM - so a construction's content can be identified, compared and named before a single
        /// TBD object is read or created.
        /// <para>
        /// This is the same resolution the direct export has always performed, lifted out of the write: the
        /// transparent/opaque decision, the part's ordered layer list, each layer's material looked up in the
        /// model's <c>MaterialLibrary</c>, and each layer's stated thickness. It mirrors the write exactly
        /// where the write is selective - a layer whose material the library does not hold is SKIPPED, as it
        /// always has been, so the definition describes the layers TBD will really end up with rather than
        /// the ones the model listed.
        /// </para>
        /// <para>
        /// <b>What is not here is what the direct export does not write.</b> A construction it creates carries
        /// no description and no additional heat transfer, so both are stated as absent - and that is exactly
        /// what keeps a pre-existing construction that DOES carry one from being adopted in its place.
        /// </para>
        /// <para>
        /// <b>A layer whose material cannot be predicted leaves the definition unproven</b>
        /// (<see cref="ConstructionDefinition.Proven"/>) rather than refused: the construction is still
        /// created exactly as before, it simply never takes part in reuse. See
        /// <see cref="ConstructionMaterialDefinition(Core.IMaterial)"/>.
        /// </para>
        /// </summary>
        /// <param name="apertureConstruction">The SAM aperture construction.</param>
        /// <param name="aperturePart">Which half of the window to resolve.</param>
        /// <param name="materialLibrary">The model's material library - the one the write will look layers up in.</param>
        /// <param name="refusal">Why no definition could be resolved, or null on success.</param>
        public static ConstructionDefinition ConstructionDefinition(this ApertureConstruction apertureConstruction, Analytical.AperturePart aperturePart, Core.MaterialLibrary materialLibrary, out string refusal)
        {
            refusal = null;

            if (apertureConstruction == null)
            {
                refusal = "No aperture construction to resolve a TBD construction from.";
                return null;
            }

            if (aperturePart != Analytical.AperturePart.Pane && aperturePart != Analytical.AperturePart.Frame)
            {
                refusal = string.Format("An aperture construction is either a pane or a frame; '{0}' is neither.", aperturePart);
                return null;
            }

            //Exactly the type the write sets: transparent when the part's materials say so, and otherwise
            //left at TBD's own opaque default rather than assigned.
            int type = apertureConstruction.Transparent(materialLibrary, aperturePart)
                ? (int)TBD.ConstructionTypes.tcdTransparentConstruction
                : (int)TBD.ConstructionTypes.tcdOpaqueConstruction;

            List<ConstructionLayerDefinition> constructionLayerDefinitions = new List<ConstructionLayerDefinition>();

            List<ConstructionLayer> constructionLayers = apertureConstruction.GetConstructionLayers(aperturePart);
            if (constructionLayers != null)
            {
                foreach (ConstructionLayer constructionLayer in constructionLayers)
                {
                    //`as Core.Material` and the skip on null are the write's own predicate, not a
                    //simplification of it: a layer the library cannot resolve never reaches the TBD.
                    Core.Material material = materialLibrary?.GetMaterial(constructionLayer?.Name) as Core.Material;
                    if (material == null)
                    {
                        continue;
                    }

                    float width = global::System.Convert.ToSingle(constructionLayer.Thickness);

                    //One stated thickness, written to both places TBD keeps a layer width.
                    constructionLayerDefinitions.Add(new ConstructionLayerDefinition(ConstructionMaterialDefinition(material), width, width));
                }
            }

            return new ConstructionDefinition(aperturePart, type, 0, null, constructionLayerDefinitions);
        }
    }
}
