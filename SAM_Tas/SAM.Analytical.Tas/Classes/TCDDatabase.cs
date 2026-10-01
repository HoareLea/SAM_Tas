// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;
using TCD;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// Reads a TCD database and converts it to a SAM ConstructionManager.
    ///
    /// TCD.exe is an out-of-process COM server, so reading is the expensive part: every property
    /// of every material and construction is read once, in Load, into plain managed snapshots.
    /// Everything after that (unique names, conversion) works on the snapshots and never calls TCD.
    /// The unique names are computed in a single pass over the loaded objects, and the names of the
    /// materials already added to the destination library are tracked in a set, so the work is
    /// linear in the size of the database (it used to be quadratic: every name was recomputed by
    /// re-reading every object, and every material was looked up in the growing library, which
    /// clones the whole library per lookup).
    ///
    /// The result is identical to the previous implementation: same names (the n-th object, in
    /// load order, sharing a name becomes "name n"), same order, same duplicate handling.
    /// </summary>
    internal class TCDDatabase
    {
        private sealed class ConstructionData
        {
            public string Name;
            public string Description;
            public float AdditionalHeatTransfer;
            public List<TCDMaterialData> Materials = new List<TCDMaterialData>();
            public List<float> Widths = new List<float>();
        }

        private Dictionary<string, ConstructionData> constructions;
        private Dictionary<string, TCDMaterialData> materials;

        private Dictionary<string, List<string>> layersDictionary;

        private Dictionary<string, Category> materialCategories;
        private Dictionary<string, Category> constructionCategories;

        private Dictionary<string, string> constructionNames;
        private Dictionary<string, string> materialNames;

        public TCDDatabase()
        {

        }
        
        public TCDDatabase(Document document)
        {
            Load(document);
        }

        public void Load(Document document)
        {
            constructions = new Dictionary<string, ConstructionData>();
            materials = new Dictionary<string, TCDMaterialData>();
            layersDictionary = new Dictionary<string, List<string>>();
            materialCategories = new Dictionary<string, Category>();
            constructionCategories = new Dictionary<string, Category>();
            constructionNames = new Dictionary<string, string>();
            materialNames = new Dictionary<string, string>();

            if (document == null)
            {
                return;
            }

            AddRange(document.materialRoot);
            AddRange(document.constructionRoot);

            UpdateUniqueNames();
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
                    ConstructionData constructionData = new ConstructionData();
                    constructionData.Name = construction.name;
                    constructionData.Description = construction.description;
                    constructionData.AdditionalHeatTransfer = construction.additionalHeatTransfer;

                    constructions[guid] = constructionData;
                    constructionCategories[guid] = category;

                    List<string> layers = new List<string>();

                    int index_Material = 1;
                    material material = construction.materials(index_Material);
                    while (material != null)
                    {
                        TCDMaterialData materialData = new TCDMaterialData(material);

                        string uniqueId = materialData.UniqueId();
                        materials[uniqueId] = materialData;
                        layers.Add(uniqueId);

                        constructionData.Materials.Add(materialData);
                        constructionData.Widths.Add(construction.materialWidth[index_Material]);

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
                TCDMaterialData materialData = new TCDMaterialData(material);

                string uniqueId = materialData.UniqueId();
                materials[uniqueId] = materialData;
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

            // Names of the materials already in the destination library (the library is keyed by
            // material name); extended as materials are added, so it is never queried again.
            HashSet<string> materialNames_Added = new HashSet<string>();
            List<Core.IMaterial> materials_Existing = constructionManager.MaterialLibrary?.GetMaterials();
            if (materials_Existing != null)
            {
                foreach (Core.IMaterial material in materials_Existing)
                {
                    materialNames_Added.Add(material.Name);
                }
            }

            if(constructions != null)
            {
                foreach(KeyValuePair<string, ConstructionData> keyValuePair in constructions)
                {
                    Update(constructionManager, keyValuePair.Key, keyValuePair.Value, materialNames_Added);
                }
            }

            if(materials != null)
            {
                foreach (KeyValuePair<string, TCDMaterialData> keyValuePair in materials)
                {
                    Update(constructionManager, keyValuePair.Key, keyValuePair.Value, materialNames_Added);
                }
            }
        }

        private Construction Update(ConstructionManager constructionManager, string guid, ConstructionData constructionData, HashSet<string> materialNames_Added)
        {
            if(constructionManager == null || constructionData == null)
            {
                return null;
            }

            string uniqueName = constructionNames[guid];

            List<ConstructionLayer> constructionLayers = new List<ConstructionLayer>();

            List<string> uniqueIds = layersDictionary[guid];
            for(int i = 0; i < constructionData.Materials.Count; i++)
            {
                TCDMaterialData material = constructionData.Materials[i];

                float width = constructionData.Widths[i];
                if(width == 0)
                {
                    width = material.Width;
                }

                string uniqueName_Material = materialNames[uniqueIds[i]];
                constructionLayers.Add(new ConstructionLayer(uniqueName_Material, width));

                if(!materialNames_Added.Contains(uniqueName_Material))
                {
                    Add(constructionManager, material.ToSAM(uniqueName_Material), materialNames_Added);
                }
            }

            Construction result = new Construction(uniqueName, constructionLayers);

            if(constructionCategories != null && constructionCategories.TryGetValue(guid, out Category category) && category != null)
            {
                result.SetValue(ParameterizedSAMObjectParameter.Category, new Category(category));
            }

            string description = constructionData.Description;
            if (!string.IsNullOrEmpty(description))
            {
                result.SetValue(Analytical.ConstructionParameter.Description, description);
            }

            double additionalHeatTransfer = constructionData.AdditionalHeatTransfer;
            if (!double.IsNaN(additionalHeatTransfer) && additionalHeatTransfer != 0)
            {
                result.SetValue(ConstructionParameter.AdditionalHeatTransfer, additionalHeatTransfer);
            }

            constructionManager.Add(result);

            return result;
        }

        private void Update(ConstructionManager constructionManager, string uniqueId, TCDMaterialData material, HashSet<string> materialNames_Added)
        {
            if (constructionManager == null || material == null)
            {
                return;
            }

            string uniqueName = materialNames[uniqueId];
            if(uniqueName == null || materialNames_Added.Contains(uniqueName))
            {
                return;
            }

            Core.IMaterial result = material.ToSAM(uniqueName);

            if (materialCategories != null && materialCategories.TryGetValue(uniqueId, out Category category) && category != null)
            {
                result.SetValue(ParameterizedSAMObjectParameter.Category, new Category(category));
            }

            Add(constructionManager, result, materialNames_Added);
        }

        private static void Add(ConstructionManager constructionManager, Core.IMaterial material, HashSet<string> materialNames_Added)
        {
            if (constructionManager.Add(material))
            {
                materialNames_Added.Add(material.Name);
            }
        }

        /// <summary>
        /// Unique names for all constructions and materials, in one pass each over the loaded
        /// objects in load order: the n-th object sharing a name gets "name n", the first keeps it.
        /// </summary>
        private void UpdateUniqueNames()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (KeyValuePair<string, ConstructionData> keyValuePair in constructions)
            {
                constructionNames[keyValuePair.Key] = UniqueName(keyValuePair.Value.Name, counts);
            }

            counts.Clear();
            foreach (KeyValuePair<string, TCDMaterialData> keyValuePair in materials)
            {
                materialNames[keyValuePair.Key] = UniqueName(keyValuePair.Value.Name, counts);
            }
        }

        private static string UniqueName(string name, Dictionary<string, int> counts)
        {
            if (name == null)
            {
                name = string.Empty;
            }

            counts.TryGetValue(name, out int count);
            count++;
            counts[name] = count;

            return count == 1 ? name : string.Format("{0} {1}", name, count);
        }
    }
}
