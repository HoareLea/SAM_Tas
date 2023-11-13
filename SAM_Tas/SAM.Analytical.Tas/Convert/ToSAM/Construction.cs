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
            
            string description = construction.description;
            if (!string.IsNullOrEmpty(description))
            {
                result.SetValue(ConstructionParameter.Description, description);
            }

            return result;
        }

        public static Construction ToSAM(this TCD.Construction construction, double tolerance = Core.Tolerance.MicroDistance)
        {
            if (construction == null)
            {
                return null;
            }

            List<ConstructionLayer> constructionLayers = ToSAM_ConstructionLayers(construction, tolerance);

            Construction result = new Construction(construction.name, constructionLayers);

            string description = construction.description;
            if(!string.IsNullOrEmpty(description))
            {
                result.SetValue(ConstructionParameter.Description, description);
            }

            return result;
        }
    }
}
