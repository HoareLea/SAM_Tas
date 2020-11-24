using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateZones(this TBD.Building building, IEnumerable<Space> spaces, ProfileLibrary profileLibrary)
        {
            if (building == null || spaces == null || profileLibrary == null)
                return false;

            List<TBD.zone> result = new List<TBD.zone>();
            foreach (Space space in spaces)
                result.Add(UpdateZone(building, space, profileLibrary));

            return result != null && result.Count > 0;
        }

        public static bool UpdateZones(this TBD.Building building, AdjacencyCluster adjacencyCluster, ProfileLibrary profileLibrary)
        {
            return UpdateZones(building, adjacencyCluster?.GetSpaces(), profileLibrary);
        }

        public static bool UpdateZones(this TBD.Building building, AnalyticalModel analyticalModel)
        {
            return UpdateZones(building, analyticalModel?.AdjacencyCluster, analyticalModel.ProfileLibrary);
        }

        public static bool UpdateZones(this AnalyticalModel analyticalModel, SAMTBDDocument sAMTBDDocument)
        {
            if (analyticalModel == null || sAMTBDDocument == null)
                return false;

            return UpdateZones(sAMTBDDocument.TBDDocument?.Building, analyticalModel);
        }

        public static bool UpdateZones(this AnalyticalModel analyticalModel, string path_TBD)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateZones(analyticalModel, sAMTBDDocument);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }
    }
}