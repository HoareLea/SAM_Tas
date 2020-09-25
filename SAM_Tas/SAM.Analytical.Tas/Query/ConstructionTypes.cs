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

            MaterialType materialType = Analytical.Query.MaterialType(constructionLayers, materialLibrary);
            if (materialType == MaterialType.Undefined)
                return TBD.ConstructionTypes.tcdOpaqueConstruction;

            switch(materialType)
            {
                case MaterialType.Opaque:
                    return TBD.ConstructionTypes.tcdOpaqueConstruction;
                case MaterialType.Transparent:
                    return TBD.ConstructionTypes.tcdTransparentConstruction;
            }

            return TBD.ConstructionTypes.tcdTransparentConstruction;
        }
    }
}