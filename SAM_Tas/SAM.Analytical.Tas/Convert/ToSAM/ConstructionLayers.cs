using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<ConstructionLayer> ToSAM_ConstructionLayers(this TBD.Construction construction)
        {
            if(construction == null)
            {
                return null;
            }

            List<TBD.material> materials = construction.Materials();

            List<ConstructionLayer> constructionLayers = null;
            if (materials != null)
            {
                constructionLayers = new List<ConstructionLayer>();
                for (int i = 0; i < materials.Count; i++)
                {
                    TBD.material material = materials[i];
                    double thickness = construction.materialWidth[i + 1];
                    if (double.IsNaN(thickness) || thickness == 0.0)
                    {
                        thickness = material.width;
                    }

                    constructionLayers.Add(new ConstructionLayer(material.name, thickness));
                }
            }

            return constructionLayers;
        }

        public static List<ConstructionLayer> ToSAM_ConstructionLayers(this TCD.Construction construction, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (construction == null)
            {
                return null;
            }

            List<TCD.material> materials = construction.Materials();

            List<ConstructionLayer> constructionLayers = null;
            if (materials != null)
            {
                constructionLayers = new List<ConstructionLayer>();
                for (int i = 0; i < materials.Count; i++)
                {
                    TCD.material material = materials[i];
                    double thickness = construction.materialWidth[i + 1];
                    if(double.IsNaN(thickness) || thickness == 0.0)
                    {
                        thickness = material.width;
                    }

                    thickness = Core.Query.Round(thickness, tolerance);

                    constructionLayers.Add(new ConstructionLayer(material.name, thickness));
                }
            }

            return constructionLayers;
        }
    }
}
