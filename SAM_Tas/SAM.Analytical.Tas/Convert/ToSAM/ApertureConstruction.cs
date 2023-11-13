using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static ApertureConstruction ToSAM_ApertureConstruction(this TBD.Construction construction, ApertureType apertureType)
        {
            if(construction == null)
            {
                return null;
            }

            List<ConstructionLayer> constructionLayers = ToSAM_ConstructionLayers(construction);

            List<ConstructionLayer> constructionLayers_Pane = null;
            List<ConstructionLayer> constructionLayers_Frame = null;

            string name = construction.name?.Trim();
            if(!string.IsNullOrWhiteSpace(name))
            {
                if (name.EndsWith(AperturePart.Frame.Sufix()))
                {
                    name = name.Substring(0, name.Length - AperturePart.Frame.Sufix().Length - 1).Trim();
                    constructionLayers_Frame = constructionLayers;

                }
                else if (name.EndsWith(AperturePart.Pane.Sufix()))
                {
                    name = name.Substring(0, name.Length - AperturePart.Pane.Sufix().Length - 1).Trim();
                    constructionLayers_Pane = constructionLayers;
                }
                else
                {
                    constructionLayers_Pane = constructionLayers;
                }
            }
            else
            {
                constructionLayers_Pane = constructionLayers;
            }

            ApertureConstruction result = new ApertureConstruction(System.Guid.NewGuid(), name, apertureType, constructionLayers_Pane, constructionLayers_Frame);
            return result;
        }

        public static ApertureConstruction ToSAM_ApertureConstruction(this TCD.Construction construction, ApertureType apertureType, double tolerance = Core.Tolerance.MacroDistance)
        {
            if (construction == null)
            {
                return null;
            }

            List<ConstructionLayer> constructionLayers = ToSAM_ConstructionLayers(construction, tolerance);

            List<ConstructionLayer> constructionLayers_Pane = null;
            List<ConstructionLayer> constructionLayers_Frame = null;

            string name = construction.name?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (name.EndsWith(AperturePart.Frame.Sufix()))
                {
                    name = name.Substring(0, name.Length - AperturePart.Frame.Sufix().Length - 1).Trim();
                    constructionLayers_Frame = constructionLayers;

                }
                else if (name.EndsWith(AperturePart.Pane.Sufix()))
                {
                    name = name.Substring(0, name.Length - AperturePart.Pane.Sufix().Length - 1).Trim();
                    constructionLayers_Pane = constructionLayers;
                }
                else
                {
                    constructionLayers_Pane = constructionLayers;
                }
            }
            else
            {
                constructionLayers_Pane = constructionLayers;
            }

            ApertureConstruction result = new ApertureConstruction(System.Guid.NewGuid(), name, apertureType, constructionLayers_Pane, constructionLayers_Frame);
            return result;
        }
    }
}
