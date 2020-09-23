using SAM.Core;
using System.Collections.Generic;
using System.Linq;

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

            return UpdateConstruction(construction_TBD, construction.ConstructionLayers, materialLibrary);
        }

        public static bool UpdateConstruction(this TBD.Construction construction_TBD, IEnumerable<ConstructionLayer> constructionLayers, MaterialLibrary materialLibrary)
        {
            List<ConstructionLayer> constructionLayers_TBD = construction_TBD.ConstructionLayers();

            if (constructionLayers_TBD == null && constructionLayers == null)
                return false;

            bool update = true;

            if (constructionLayers_TBD != null && constructionLayers != null)
            {
                if (constructionLayers_TBD.Count == constructionLayers.Count())
                {
                    update = false;
                    for (int i = 0; i < constructionLayers.Count(); i++)
                    {
                        if (!constructionLayers.ElementAt(i).Name.Equals(constructionLayers_TBD[i].Name) || !constructionLayers.ElementAt(i).Thickness.Equals(constructionLayers_TBD[i].Thickness))
                        {
                            update = true;
                            break;
                        }
                    }
                }
            }

            if (update)
            {
                construction_TBD.RemoveMaterials();
                for (int i = 0; i < constructionLayers.Count(); i++)
                {
                    TBD.material material_TBD = construction_TBD.AddMaterial();
                    construction_TBD.materialWidth[i] = System.Convert.ToSingle(constructionLayers.ElementAt(i).Thickness);

                    string name = constructionLayers.ElementAt(i).Name;

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