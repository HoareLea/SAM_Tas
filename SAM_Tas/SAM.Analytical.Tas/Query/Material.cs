using SAM.Core;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static IMaterial Material(this LayerThicknessCalculationResult layerThicknessCalculationResult, ConstructionManager constructionManager)
        {
            if(layerThicknessCalculationResult == null || constructionManager == null)
            {
                return null;
            }

            string name = ConstructionLayer(layerThicknessCalculationResult, constructionManager)?.Name;
            if(string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return constructionManager.GetMaterial(name);
        }

        public static IMaterial Material(this LayerThicknessCalculationData layerThicknessCalculationData, ConstructionManager constructionManager)
        {
            if (layerThicknessCalculationData == null || constructionManager == null)
            {
                return null;
            }

            Construction construction = constructionManager.GetConstructions(layerThicknessCalculationData.ConstructionName)?.FirstOrDefault();
            if (construction == null)
            {
                return null;
            }

            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if (constructionLayers == null || constructionLayers.Count == 0)
            {
                return null;
            }

            if (constructionLayers.Count <= layerThicknessCalculationData.LayerIndex)
            {
                return null;
            }

            string name = constructionLayers[layerThicknessCalculationData.LayerIndex]?.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return constructionManager.GetMaterial(name);
        }
    }
}