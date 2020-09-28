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

            adjacencyCluster.UpdateConstructionsPanelTypes();

            List<SAMType> result = new List<SAMType>();

            List<Construction> constructions = adjacencyCluster.GetConstructions();
            if (constructions != null && constructions.Count != 0)
            {
                constructions = UpdateConstructions(building, constructions, analyticalModel.MaterialLibrary);
                if (constructions != null && constructions.Count != 0)
                    constructions.ForEach(x => result.Add(x));
            }

            List<ApertureConstruction> apertureConstructions = analyticalModel.AdjacencyCluster?.GetApertureConstructions();
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

                string uniqueName = construction.UniqueName();
                if (string.IsNullOrWhiteSpace(uniqueName))
                    continue;

                TBD.Construction construction_TBD = building.GetConstructionByName(uniqueName);
                if(construction_TBD == null)
                {
                    construction_TBD = building.AddConstruction(null);
                    construction_TBD.name = uniqueName;
                }

                ConstructionTypes constructionTypes = ConstructionTypes.tcdOpaqueConstruction;
                List<ConstructionLayer> constructionLayers = construction.ConstructionLayers;
                if (constructionLayers != null && constructionLayers.Count != 0)
                    constructionTypes = Query.ConstructionTypes(constructionLayers, materialLibrary);

                construction_TBD.type = constructionTypes;

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

                string uniqueName = apertureConstruction.UniqueName();
                if (string.IsNullOrWhiteSpace(uniqueName))
                    continue;

                TBD.Construction construction_TBD = null;
                List<ConstructionLayer> constructionLayers = null;
                ConstructionTypes constructionTypes;


                //Pane Construction
                string paneApertureConstructionUniqueName = apertureConstruction.PaneApertureConstructionUniqueName();
                construction_TBD = building.GetConstructionByName(paneApertureConstructionUniqueName);
                if (construction_TBD == null)
                {
                    construction_TBD = building.AddConstruction(null);
                    construction_TBD.name = paneApertureConstructionUniqueName;
                }

                constructionTypes = ConstructionTypes.tcdTransparentConstruction;
                constructionLayers = apertureConstruction.PaneConstructionLayers;
                if (constructionLayers != null && constructionLayers.Count != 0)
                    constructionTypes = Query.ConstructionTypes(constructionLayers, materialLibrary);

                construction_TBD.type = constructionTypes;

                if (construction_TBD.UpdateConstruction(constructionLayers, materialLibrary))
                    result.Add(apertureConstruction);


                //Frame Construction
                string frameApertureConstructionUniqueName = apertureConstruction.FrameApertureConstructionUniqueName();
                construction_TBD = building.GetConstructionByName(frameApertureConstructionUniqueName);
                if (construction_TBD == null)
                {
                    construction_TBD = building.AddConstruction(null);
                    construction_TBD.name = frameApertureConstructionUniqueName;
                }

                constructionTypes = ConstructionTypes.tcdOpaqueConstruction;
                constructionLayers = apertureConstruction.FrameConstructionLayers;
                if(constructionLayers != null && constructionLayers.Count != 0)
                    constructionTypes = Query.ConstructionTypes(constructionLayers, materialLibrary);

                construction_TBD.type = constructionTypes;

                if (construction_TBD.UpdateConstruction(constructionLayers, materialLibrary))
                    result.Add(apertureConstruction);
            }

            return result;
        }
    }
}