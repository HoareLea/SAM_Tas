using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static List<TCD.Construction> ToTCD_Constructions(this ApertureConstruction apertureConstruction, TCD.Document tcdDocument, AnalyticalModel analyticalModel)
        {
            if (tcdDocument == null || apertureConstruction == null)
            {
                return null;
            }

            return ToTCD_Constructions(apertureConstruction, tcdDocument.constructionRoot, analyticalModel?.ConstructionManager);
        }

        public static List<TCD.Construction> ToTCD_Constructions(this ApertureConstruction apertureConstruction, TCD.Document tcdDocument, ConstructionManager constructionManager)
        {
            if (tcdDocument == null || apertureConstruction == null)
            {
                return null;
            }

            return ToTCD_Constructions(apertureConstruction, tcdDocument.constructionRoot, constructionManager);
        }

        public static List<TCD.Construction> ToTCD_Constructions(this ApertureConstruction apertureConstruction, TCD.ConstructionFolder constructionFolder, ConstructionManager constructionManager)
        {
            if(apertureConstruction == null || constructionFolder == null)
            {
                return null;
            }

            MaterialLibrary materialLibrary = constructionManager?.MaterialLibrary;
            if (materialLibrary == null)
            {
                return null;
            }

            if (!apertureConstruction.TryGetValue(Analytical.ApertureConstructionParameter.Description, out string description) || string.IsNullOrWhiteSpace(description))
            {
                description = null;
            }

            List<TCD.Construction> result = new List<TCD.Construction>();

            List<ConstructionLayer> constructionLayers = null;

            constructionLayers = apertureConstruction.PaneConstructionLayers;
            if(constructionLayers != null && constructionLayers.Count != 0)
            {
                TCD.Construction construction = constructionFolder.AddConstruction();
                construction.name = apertureConstruction.Name + " -pane";
                construction.description = description;
                bool transparent = Analytical.Query.Transparent(constructionLayers, materialLibrary);
                construction.type = transparent ? TCD.ConstructionTypes.tcdTransparentConstruction : TCD.ConstructionTypes.tcdOpaqueConstruction;

                foreach (ConstructionLayer constructionLayer in constructionLayers)
                {
                    if (constructionLayer == null)
                    {
                        continue;
                    }

                    IMaterial material = materialLibrary.GetMaterial(constructionLayer.Name);
                    TCD.material material_TCD = construction.AddMaterial();
                    if (material_TCD != null)
                    {
                        Modify.UpdateMaterial(material_TCD, material);
                        material_TCD.width = System.Convert.ToSingle(constructionLayer.Thickness);
                    }
                }

                if(apertureConstruction.TryGetValue(ApertureConstructionParameter.PaneAdditionalHeatTransfer, out double additionalHeatTransfer) && !double.IsNaN(additionalHeatTransfer))
                {
                    construction.additionalHeatTransfer = System.Convert.ToSingle(additionalHeatTransfer);
                }

                result.Add(construction);
            }

            constructionLayers = apertureConstruction.FrameConstructionLayers;
            if (constructionLayers != null && constructionLayers.Count != 0)
            {
                TCD.Construction construction = constructionFolder.AddConstruction();
                construction.name = apertureConstruction.Name + " -frame";
                construction.description = description;
                bool transparent = Analytical.Query.Transparent(constructionLayers, materialLibrary);
                construction.type = transparent ? TCD.ConstructionTypes.tcdTransparentConstruction : TCD.ConstructionTypes.tcdOpaqueConstruction;

                foreach (ConstructionLayer constructionLayer in constructionLayers)
                {
                    if (constructionLayer == null)
                    {
                        continue;
                    }

                    IMaterial material = materialLibrary.GetMaterial(constructionLayer.Name);
                    TCD.material material_TCD = construction.AddMaterial();
                    if (material_TCD != null)
                    {
                        Modify.UpdateMaterial(material_TCD, material);
                        material_TCD.width = System.Convert.ToSingle(constructionLayer.Thickness);
                    }
                }

                if (apertureConstruction.TryGetValue(ApertureConstructionParameter.FrameAdditionalHeatTransfer, out double additionalHeatTransfer) && !double.IsNaN(additionalHeatTransfer))
                {
                    construction.additionalHeatTransfer = System.Convert.ToSingle(additionalHeatTransfer);
                }

                result.Add(construction);
            }


            return result;
        }
    }
}
