using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Convert
    {
        public static TCD.Construction ToTCD(this Construction construction, TCD.Document tcdDocument, AnalyticalModel analyticalModel)
        {
            if (tcdDocument == null || construction == null)
            {
                return null;
            }

            return ToTCD(construction, tcdDocument.constructionRoot, analyticalModel?.ConstructionManager);
        }

        public static TCD.Construction ToTCD(this Construction construction, TCD.Document tcdDocument, ConstructionManager constructionManager)
        {
            if (tcdDocument == null || construction == null)
            {
                return null;
            }

            return ToTCD(construction, tcdDocument.constructionRoot, constructionManager);
        }

        public static TCD.Construction ToTCD(this Construction construction, TCD.ConstructionFolder constructionFolder, ConstructionManager constructionManager)
        {
            if(construction == null || constructionFolder == null)
            {
                return null;
            }

            MaterialLibrary materialLibrary = constructionManager?.MaterialLibrary;
            if (materialLibrary == null)
            {
                return null;
            }

            TCD.Construction result = constructionFolder.AddConstruction();
            result.name = construction.Name;

            bool transparent = Analytical.Query.Transparent(construction, materialLibrary);

            result.type = transparent ? TCD.ConstructionTypes.tcdTransparentConstruction : TCD.ConstructionTypes.tcdOpaqueConstruction;

            if(construction.TryGetValue(Analytical.ConstructionParameter.Description, out string description) && !string.IsNullOrWhiteSpace(description))
            {
                result.description = description;
            }

            if (construction.TryGetValue(ConstructionParameter.AdditionalHeatTransfer, out double additionalHeatTransfer) && !double.IsNaN(additionalHeatTransfer))
            {
                result.additionalHeatTransfer = System.Convert.ToSingle(additionalHeatTransfer);
            }

            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
            if(constructionLayers != null && constructionLayers.Count != 0)
            {
                foreach (ConstructionLayer constructionLayer in constructionLayers)
                {
                    if(constructionLayer == null)
                    {
                        continue;
                    }

                    IMaterial material = materialLibrary.GetMaterial(constructionLayer.Name);

                    TCD.material material_TCD = result.AddMaterial();
                    if(material_TCD != null)
                    {
                        Modify.UpdateMaterial(material_TCD, material);
                        material_TCD.width = System.Convert.ToSingle(constructionLayer.Thickness);
                    }
                }
            }
            
            return result;
        }
    }
}
