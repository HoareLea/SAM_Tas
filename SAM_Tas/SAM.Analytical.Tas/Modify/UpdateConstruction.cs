using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateConstruction(this TBD.Construction construction_TBD, Construction construction, MaterialLibrary materialLibrary)
        {
            if (construction == null || construction_TBD == null)
                return false;

            if (!construction.Name.Equals(construction_TBD.name))
                construction_TBD.name = construction.Name;

            List<ConstructionLayer> constructionLayers_TBD = construction_TBD.ConstructionLayers();
            List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;

            if (constructionLayers_TBD == null && constructionLayers == null)
                return false;

            bool update = true;

            if(constructionLayers_TBD != null && constructionLayers != null)
            {
                if(constructionLayers_TBD.Count == constructionLayers.Count)
                {
                    update = false;
                    for(int i =0; i < constructionLayers.Count; i++)
                    {
                        if(!constructionLayers[i].Name.Equals(constructionLayers_TBD[i].Name) || !constructionLayers[i].Thickness.Equals(constructionLayers_TBD[i].Thickness))
                        {
                            update = true;
                            break;
                        }
                    }
                }
            }

            if(update)
            {
                construction_TBD.RemoveMaterials();
                for(int i =0; i < constructionLayers.Count; i++)
                {
                    TBD.material material_TBD = construction_TBD.AddMaterial();
                    construction_TBD.materialWidth[i + 1] = System.Convert.ToSingle(constructionLayers[i].Thickness);

                    string name = constructionLayers[i].Name;

                    material_TBD.name = name;

                    IMaterial material = materialLibrary?.GetMaterial(name);
                    if (material == null)
                        continue;

                    material_TBD.UpdateMaterial(material);
                }
            }

            return update;
        }
    }
}