using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Construction> UpdateConstructions(this string path_TBD, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<Construction> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateConstructions(sAMTBDDocument, analyticalModel);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Construction> UpdateConstructions(this SAMTBDDocument sAMTBDDocument, AnalyticalModel analyticalModel)
        {
            if (sAMTBDDocument == null)
                return null;

            return UpdateConstructions(sAMTBDDocument.TBDDocument, analyticalModel);
        }

        public static List<Construction> UpdateConstructions(this TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
                return null;

            Building building = tBDDocument.Building;
            if (building == null)
                return null;

            List<Construction> constructions = analyticalModel.AdjacencyCluster?.GetConstructions();
            if (constructions == null || constructions.Count == 0)
                return null;

            return UpdateConstructions(building, constructions, analyticalModel.MaterialLibrary);

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
    }
}