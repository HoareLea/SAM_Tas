using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<SAMType> UpdateConstructions(this string path_TBD, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<SAMType> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateConstructions(sAMTBDDocument, analyticalModel);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<SAMType> UpdateConstructions(this SAMTBDDocument sAMTBDDocument, AnalyticalModel analyticalModel)
        {
            if (sAMTBDDocument == null)
                return null;

            return UpdateConstructions(sAMTBDDocument.TBDDocument, analyticalModel);
        }

        public static List<SAMType> UpdateConstructions(this TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
                return null;

            Building building = tBDDocument.Building;
            if (building == null)
                return null;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
                return null;

            adjacencyCluster.SetConstructionsDefaultPanelType();

            List<SAMType> result = new List<SAMType>();

            List<Construction> constructions = adjacencyCluster.GetConstructions();
            if (constructions != null && constructions.Count != 0)
            {
                List<Construction> constructions_Temp = UpdateConstructions(building, constructions, analyticalModel.MaterialLibrary);
                if (constructions_Temp != null && constructions_Temp.Count != 0)
                    constructions_Temp.ForEach(x => result.Add(x));
            }

            List<ApertureConstruction> apertureConstructions = adjacencyCluster.GetApertureConstructions();
            if(apertureConstructions != null)
            {
                for(int i =0; i < apertureConstructions.Count; i++)
                {
                    ApertureConstruction apertureConstruction = apertureConstructions[i];

                    string name = Query.Name(apertureConstruction.UniqueName(), true, true, false, false);

                    apertureConstructions[i] = new ApertureConstruction(apertureConstruction, name);
                }
            }

            if (apertureConstructions != null && apertureConstructions.Count != 0)
            {
                apertureConstructions = UpdateConstructions(building, apertureConstructions, analyticalModel.MaterialLibrary);
                if (apertureConstructions != null && apertureConstructions.Count != 0)
                    apertureConstructions.ForEach(x => result.Add(x));
            }

            return result;
        }

        public static List<Construction> UpdateConstructions(this Building building, IEnumerable<Construction> constructions, MaterialLibrary materialLibrary)
        {
            if (constructions == null || building == null)
                return null;

            List<Construction> result = new List<Construction>();
            foreach(Construction construction in constructions)
            {
                if (construction == null)
                    continue;

                string name = Query.Name(construction.UniqueName(), true, true, false, false);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                if (constructionLayers == null || constructionLayers.Count == 0)
                    continue;

                TBD.Construction construction_TBD = building.GetConstructionByName(name);
                if(construction_TBD == null)
                {
                    construction_TBD = building.AddConstruction(null);
                    construction_TBD.name = name;
                }

                construction_TBD.type = Query.ConstructionTypes(constructionLayers, materialLibrary);

                if (construction_TBD.UpdateConstruction(constructionLayers, materialLibrary))
                    result.Add(construction);
            }

            return result;
        }

        public static List<ApertureConstruction> UpdateConstructions(this Building building, IEnumerable<ApertureConstruction> apertureConstructions, MaterialLibrary materialLibrary)
        {
            if (apertureConstructions == null || building == null)
                return null;

            List<ApertureConstruction> result = new List<ApertureConstruction>();
            foreach (ApertureConstruction apertureConstruction in apertureConstructions)
            {
                if (apertureConstruction == null)
                    continue;

                string name = Query.Name(apertureConstruction.UniqueName(), true, true, false, false);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                TBD.Construction construction_TBD = null;
                List<ConstructionLayer> constructionLayers = null;
                ConstructionTypes constructionTypes;

                double thickness = double.NaN;

                //Pane Construction
                thickness = apertureConstruction.GetPaneThickness();
                if(!double.IsNaN(thickness) && thickness > 0)
                {
                    string paneName = Analytical.Query.PaneApertureConstructionUniqueName(name);
                    construction_TBD = building.GetConstructionByName(paneName);
                    if (construction_TBD == null)
                    {
                        construction_TBD = building.AddConstruction(null);
                        construction_TBD.name = paneName;
                    }

                    constructionTypes = ConstructionTypes.tcdTransparentConstruction;
                    constructionLayers = apertureConstruction.PaneConstructionLayers;
                    if (constructionLayers != null && constructionLayers.Count != 0)
                        constructionTypes = Query.ConstructionTypes(constructionLayers, materialLibrary);

                    construction_TBD.type = constructionTypes;

                    if (construction_TBD.UpdateConstruction(constructionLayers, materialLibrary))
                        result.Add(apertureConstruction);
                }

                //Frame Construction
                thickness = apertureConstruction.GetFrameThickness();
                if (!double.IsNaN(thickness) && thickness > 0)
                {
                    string frameName = Analytical.Query.FrameApertureConstructionUniqueName(name);
                    construction_TBD = building.GetConstructionByName(frameName);
                    if (construction_TBD == null)
                    {
                        construction_TBD = building.AddConstruction(null);
                        construction_TBD.name = frameName;
                    }

                    constructionTypes = ConstructionTypes.tcdOpaqueConstruction;
                    constructionLayers = apertureConstruction.FrameConstructionLayers;

                    //Frame TBD.Construction cannot be empty
                    if (constructionLayers == null || constructionLayers.Count == 0)
                        constructionLayers = apertureConstruction.PaneConstructionLayers;

                    if (constructionLayers != null && constructionLayers.Count != 0)
                        constructionTypes = Query.ConstructionTypes(constructionLayers, materialLibrary);

                    construction_TBD.type = constructionTypes;

                    if (construction_TBD.UpdateConstruction(constructionLayers, materialLibrary))
                        result.Add(apertureConstruction);
                }
            }

            return result;
        }
    }
}