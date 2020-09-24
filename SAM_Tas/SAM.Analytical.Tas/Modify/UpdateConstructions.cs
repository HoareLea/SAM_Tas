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

            List<SAMType> result = new List<SAMType>();

            List<Construction> constructions = analyticalModel.AdjacencyCluster?.GetConstructions();
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

                string name = construction.Name;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                TBD.Construction construction_TBD = building.GetConstructionByName(name);
                if(construction_TBD == null)
                    construction_TBD = building.AddConstruction(null);

                if (construction_TBD.UpdateConstruction(construction, materialLibrary))
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

                string name = apertureConstruction.Name;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                TBD.Construction construction_TBD = null;

                string paneApertureConstructionName = apertureConstruction.PaneApertureConstructionName();
                construction_TBD = building.GetConstructionByName(paneApertureConstructionName);
                if (construction_TBD == null)
                {
                    construction_TBD = building.AddConstruction(null);
                    construction_TBD.name = paneApertureConstructionName;
                }

                construction_TBD.type = ConstructionTypes.tcdTransparentConstruction;

                if (construction_TBD.UpdateConstruction(apertureConstruction.PaneConstructionLayers, materialLibrary))
                    result.Add(apertureConstruction);

                string frameApertureConstructionName = apertureConstruction.FrameApertureConstructionName();
                construction_TBD = building.GetConstructionByName(frameApertureConstructionName);
                if (construction_TBD == null)
                {
                    construction_TBD = building.AddConstruction(null);
                    construction_TBD.name = frameApertureConstructionName;
                }

                ConstructionTypes constructionTypes = ConstructionTypes.tcdOpaqueConstruction;

                List<ConstructionLayer> constructionLayers = apertureConstruction.FrameConstructionLayers;
                if(constructionLayers != null && constructionLayers.Count != 0)
                {
                    constructionTypes = ConstructionTypes.tcdTransparentConstruction;
                    foreach (ConstructionLayer constructionLayer in constructionLayers)
                    {
                        OpaqueMaterial opaqueMaterial = constructionLayer?.Material(materialLibrary) as OpaqueMaterial;
                        if(opaqueMaterial != null)
                        {
                            constructionTypes = ConstructionTypes.tcdOpaqueConstruction;
                            break;
                        }
                    }
                }

                construction_TBD.type = constructionTypes;

                if (construction_TBD.UpdateConstruction(apertureConstruction.FrameConstructionLayers, materialLibrary))
                    result.Add(apertureConstruction);
            }

            return result;
        }
    }
}