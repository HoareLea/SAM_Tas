using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static TBD.ConstructionTypes ConstructionTypes(this IEnumerable<ConstructionLayer> constructionLayers, MaterialLibrary materialLibrary)
        {
            if (constructionLayers == null || materialLibrary == null)
                return TBD.ConstructionTypes.tcdOpaqueConstruction;

            foreach (ConstructionLayer constructionLayer in constructionLayers)
            {
                OpaqueMaterial opaqueMaterial = constructionLayer?.Material(materialLibrary) as OpaqueMaterial;
                if (opaqueMaterial != null)
                    return TBD.ConstructionTypes.tcdOpaqueConstruction;
            }

            return TBD.ConstructionTypes.tcdTransparentConstruction;
        }
    }
}