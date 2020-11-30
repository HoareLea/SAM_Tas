using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateZones(this TBD.Building building, IEnumerable<Space> spaces, ProfileLibrary profileLibrary, bool includeHDD = false)
        {
            if (building == null || spaces == null || profileLibrary == null)
                return false;

            List<TBD.zone> result = new List<TBD.zone>();
            foreach (Space space in spaces)
            {
                result.Add(UpdateZone(building, space, profileLibrary));
                if (includeHDD)
                    UpdateZone_HDD(building, space, profileLibrary);
            }

            building.description = string.Format("Delivered by SAM https://github.com/HoareLea/SAM [{0}]", System.DateTime.Now.ToString("yyyy/MM/dd"));

            return result != null && result.Count > 0;
        }

        public static bool UpdateZones(this TBD.Building building, AdjacencyCluster adjacencyCluster, ProfileLibrary profileLibrary, bool includeHDD = false)
        {
            return UpdateZones(building, adjacencyCluster?.GetSpaces(), profileLibrary, includeHDD);
        }

        public static bool UpdateZones(this TBD.Building building, AnalyticalModel analyticalModel, bool includeHDD = false)
        {
            if (analyticalModel == null || building == null)
                return false;

            building.name = analyticalModel.Name;
            
            return UpdateZones(building, analyticalModel?.AdjacencyCluster, analyticalModel.ProfileLibrary, includeHDD);
        }

        public static bool UpdateZones(this AnalyticalModel analyticalModel, SAMTBDDocument sAMTBDDocument, bool includeHDD = false)
        {
            if (analyticalModel == null || sAMTBDDocument == null)
                return false;

            return UpdateZones(sAMTBDDocument.TBDDocument?.Building, analyticalModel, includeHDD);
        }

        public static bool UpdateZones(this AnalyticalModel analyticalModel, string path_TBD, bool includeHDD = false)
        {
            if (analyticalModel == null || string.IsNullOrWhiteSpace(path_TBD))
                return false;

            bool result = false;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateZones(analyticalModel, sAMTBDDocument, includeHDD);
                if (result)
                    sAMTBDDocument.Save();
            }

            return result;
        }
    }
}