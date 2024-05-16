using SAM.Core;
using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateConstruction(this TBD.Construction construction_TBD, IEnumerable<ConstructionLayer> constructionLayers, MaterialLibrary materialLibrary)
        {
            List<ConstructionLayer> constructionLayers_TBD = construction_TBD.ConstructionLayers();

            if (constructionLayers_TBD == null && constructionLayers == null)
                return false;

            bool update = true;

            //Commented code to solve issue when only one propertie of material is modified but name remain the same
            //MD 20024.05.16
            //if (constructionLayers_TBD != null && constructionLayers != null && constructionLayers_TBD.Count == constructionLayers.Count())
            //{
            //    update = false;
            //    for (int i = 0; i < constructionLayers.Count(); i++)
            //    {
            //        if (!constructionLayers.ElementAt(i).Name.Equals(constructionLayers_TBD[i].Name) || !Core.Query.AlmostEqual(constructionLayers.ElementAt(i).Thickness, constructionLayers_TBD[i].Thickness))
            //        {
            //            update = true;
            //            break;
            //        }
            //    }
            //}

            if (update)
            {
                construction_TBD.RemoveMaterials();
                if(constructionLayers != null)
                {
                    for (int i = 0; i < constructionLayers.Count(); i++)
                    {
                        ConstructionLayer constructionLayer = constructionLayers.ElementAt(i);
                        string name = constructionLayer.Name;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        TBD.material material_TBD = construction_TBD.AddMaterial();
                        material_TBD.name = name;

                        float thickness = System.Convert.ToSingle(constructionLayer.Thickness);

                        IMaterial material = constructionLayer.Material(materialLibrary);
                        if (material != null)
                        {
                            material_TBD.UpdateMaterial(material);

                            if (material is TransparentMaterial)
                                material_TBD.width = thickness;
                        }

                        construction_TBD.materialWidth[i + 1] = thickness;

                    }
                }
            }

            return update;
        }
    }
}