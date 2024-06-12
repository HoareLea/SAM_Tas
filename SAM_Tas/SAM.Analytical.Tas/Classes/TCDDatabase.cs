using SAM.Core;
using System.Collections.Generic;
using TCD;

namespace SAM.Analytical.Tas
{
    internal class TCDDatabase
    {
        private Dictionary<string, TCD.Construction> constructions;
        private Dictionary<string, material> materials;

        private Dictionary<string, List<string>> layersDictionary;

        private Dictionary<string, Category> materialCategories;
        private Dictionary<string, Category> constructionCategories;

        public TCDDatabase()
        {

        }
        
        public TCDDatabase(Document document)
        {
            Load(document);
        }

        public void Load(Document document)
        {
            constructions = new Dictionary<string, TCD.Construction>();
            materials = new Dictionary<string, material>();
            layersDictionary = new Dictionary<string, List<string>>();
            materialCategories = new Dictionary<string, Category>();
            constructionCategories = new Dictionary<string, Category>();

            if (document == null)
            {
                return;
            }

            AddRange(document.materialRoot);
            AddRange(document.constructionRoot);
        }

        private void AddRange(ConstructionFolder constructionFolder, Category category = null)
        {
            if(constructionFolder == null)
            {
                return;
            }

            if(category == null)
            {
                category = new Category(constructionFolder.name);
            }
            else
            {
                category = Core.Create.Category(constructionFolder.name, category);
            }

            int index_Construction = 1;
            TCD.Construction construction = constructionFolder.constructions(index_Construction);
            while(construction != null)
            {
                string guid = construction.GUID;
                if(guid != null)
                {
                    constructions[guid] = construction;
                    constructionCategories[guid] = category;

                    List<string> layers = new List<string>();

                    int index_Material = 1;
                    material material = construction.materials(index_Material);
                    while (material != null)
                    {
                        string uniqueId = material.UniqueId();
                        materials[uniqueId] = material;
                        layers.Add(uniqueId);

                        index_Material++;
                        material = construction.materials(index_Material);
                    }

                    layersDictionary[guid] = layers;
                }

                index_Construction++;
                construction = constructionFolder.constructions(index_Construction);
            }

            int index_ConstructionFolder = 1;
            ConstructionFolder constructionFolder_Child = constructionFolder.childFolders(index_ConstructionFolder);
            while(constructionFolder_Child != null)
            {
                AddRange(constructionFolder_Child, category);
                index_ConstructionFolder++;
                constructionFolder_Child = constructionFolder.childFolders(index_ConstructionFolder);
            }

        }

        private void AddRange(MaterialFolder materialFolder, Category category = null)
        {
            if(materialFolder == null)
            {
                return;
            }

            if (category == null)
            {
                category = new Category(materialFolder.name);
            }
            else
            {
                category = Core.Create.Category(materialFolder.name, category);
            }

            int index_Material = 1;
            material material = materialFolder.materials(index_Material);
            while (material != null)
            {
                string uniqueId = material.UniqueId();
                materials[uniqueId] = material;
                materialCategories[uniqueId] = category;

                index_Material++;
                material = materialFolder.materials(index_Material);
            }

            int index_MaterialFolder = 1;
            MaterialFolder materialFolder_Child = materialFolder.childFolders(index_MaterialFolder);
            while (materialFolder_Child != null)
            {
                AddRange(materialFolder_Child, category);
                index_MaterialFolder++;
                materialFolder_Child = materialFolder.childFolders(index_MaterialFolder);
            }
        }

        public void Update(ConstructionManager constructionManager)
        {
            if(constructionManager == null)
            {
                return;
            }

            if(constructions != null)
            {
                foreach(KeyValuePair<string, TCD.Construction> keyValuePair in constructions)
                {
                    Update(constructionManager, keyValuePair.Value);
                }
            }

            if(materials != null)
            {
                foreach (KeyValuePair<string, material> keyValuePair in materials)
                {
                    Update(constructionManager, keyValuePair.Value);
                }
            }
        }

        private Construction Update(ConstructionManager constructionManager, TCD.Construction construction)
        {
            if(constructionManager == null || construction == null)
            {
                return null;
            }

            string uniqueName = UniqueName(construction);

            List<ConstructionLayer> constructionLayers = new List<ConstructionLayer>();

            int index = 1;
            material material = construction.materials(index);
            while(material != null)
            {
                float width = construction.materialWidth[index];
                if(width == 0)
                {
                    width = material.width;
                }

                string uniqueName_Material = UniqueName(material);
                constructionLayers.Add(new ConstructionLayer(uniqueName_Material, width));

                if(constructionManager.GetMaterial(uniqueName_Material) == null)
                {
                    constructionManager.Add(material.ToSAM(uniqueName_Material));
                }

                index++;
                material = construction.materials(index);

            }

            Construction result = new Construction(uniqueName, constructionLayers);
            
            if(constructionCategories != null && constructionCategories.TryGetValue(construction.GUID, out Category category) && category != null)
            {
                result.SetValue(ParameterizedSAMObjectParameter.Category, new Category(category));
            }

            string description = construction.description;
            if (!string.IsNullOrEmpty(description))
            {
                result.SetValue(Analytical.ConstructionParameter.Description, description);
            }

            double additionalHeatTransfer = construction.additionalHeatTransfer;
            if (!double.IsNaN(additionalHeatTransfer) && additionalHeatTransfer != 0)
            {
                result.SetValue(ConstructionParameter.AdditionalHeatTransfer, additionalHeatTransfer);
            }

            constructionManager.Add(result);

            return result;
        }

        private Core.IMaterial Update(ConstructionManager constructionManager, material material)
        {
            if (constructionManager == null || material == null)
            {
                return null;
            }

            string uniqueName = UniqueName(material);
            if(uniqueName == null)
            {
                return null;
            }

            Core.IMaterial result = constructionManager.GetMaterial(uniqueName);
            if(result != null)
            {
                return result;
            }

            result = material.ToSAM(uniqueName);

            if (materialCategories != null && materialCategories.TryGetValue(material.UniqueId(), out Category category) && category != null)
            {
                result.SetValue(ParameterizedSAMObjectParameter.Category, new Category(category));
            }

            constructionManager.Add(result);

            return result;
        }

        private string UniqueName(TCD.Construction construction)
        {
            if (construction == null)
            {
                return null;
            }

            string name = construction.name;
            if (name == null)
            {
                name = string.Empty;
            }

            //string[] values = name.Split(' ');
            //if(values != null && values.Length > 1)
            //{
            //    if(int.TryParse(values.Last(), out int index))
            //    {
            //        name = string.Join(" ", values.ToList().GetRange(0, values.Length - 1));
            //    }
            //}

            string guid = construction.GUID;

            int count = 1;
            foreach (KeyValuePair<string, TCD.Construction> keyValuePair in constructions)
            {
                if (keyValuePair.Value.name != name)
                {
                    continue;
                }

                if (keyValuePair.Key == guid)
                {
                    break;
                }

                count++;
            }

            return count == 1 ? name : string.Format("{0} {1}", name, count);
        }

        private string UniqueName(material material)
        {
            if(material == null)
            {
                return null;
            }

            string name = material.name;
            if(name == null)
            {
                name = string.Empty;
            }

            string uniqueId = material.UniqueId();

            int count = 1;
            foreach(KeyValuePair<string, material> keyValuePair in materials)
            {
                if(keyValuePair.Value.name != name)
                {
                    continue;
                }

                if(keyValuePair.Key == uniqueId)
                {
                    break;
                }

                count++;
            }

            return count == 1 ? name : string.Format("{0} {1}", name, count);
        }
    }
}
