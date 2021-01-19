using SAM.Core;
using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static AnalyticalModel UpdateDesignLoads(this string path_TBD, AnalyticalModel analyticalModel)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            AnalyticalModel result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateDesignLoads(sAMTBDDocument, analyticalModel);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static AnalyticalModel UpdateDesignLoads(this SAMTBDDocument sAMTBDDocument, AnalyticalModel analyticalModel)
        {
            if (sAMTBDDocument == null)
                return null;

            return UpdateDesignLoads(sAMTBDDocument.TBDDocument, analyticalModel);
        }

        public static AnalyticalModel UpdateDesignLoads(this TBDDocument tBDDocument, AnalyticalModel analyticalModel)
        {
            if (tBDDocument == null || analyticalModel == null)
                return null;

            Building building = tBDDocument.Building;
            if (building == null)
                return null;

            AdjacencyCluster adjacencyCluster = analyticalModel.AdjacencyCluster;
            if (adjacencyCluster == null)
                return null;

            List<Space> spaces = UpdateDesignLoads(building, adjacencyCluster.GetSpaces());
            if (spaces == null || spaces.Count == 0)
                return analyticalModel;

            foreach(Space space in spaces)
                adjacencyCluster.AddObject(space);

            return new AnalyticalModel(analyticalModel, adjacencyCluster);

        }

        public static List<Space> UpdateDesignLoads(this Building building, IEnumerable<Space> spaces)
        {
            if (spaces == null || building == null)
                return null;

            Dictionary<string, zone> zones = building.ZoneDictionary();
            if (zones == null)
                return null;

            List<Space> result = new List<Space>();
            if (zones.Count == 0)
                return result;

            foreach(Space space in spaces)
            {
                if (space == null)
                    continue;

                string name = space.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                zone zone;
                if (!zones.TryGetValue(name, out zone) || zone == null)
                    continue;

                space.SetValue(SpaceParameter.DesignHeatingLoad, zone.maxHeatingLoad);
                space.SetValue(SpaceParameter.DesignCoolingLoad, zone.maxCoolingLoad);
                result.Add(space);
            }

            return result;
        }
    }
}