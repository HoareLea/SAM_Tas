using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static Construction ToSAM(this TBD.Construction construction)
        {
            if(construction == null)
            {
                return null;
            }

            List<ConstructionLayer> constructionLayers = ToSAM_ConstructionLayers(construction);

            Construction result = new Construction(construction.name, constructionLayers);
            return result;
        }
    }
}
