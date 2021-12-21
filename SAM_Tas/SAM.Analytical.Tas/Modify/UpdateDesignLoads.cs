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

            List<Space> spaces = adjacencyCluster.GetSpaces();
            if (spaces == null || spaces.Count == 0)
                return analyticalModel;

            Dictionary<string, zone> zones = building.ZoneDictionary();
            if (zones == null)
                return null;

            foreach (Space space in spaces)
            {
                string name = space?.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                zone zone;
                if (!zones.TryGetValue(name, out zone) || zone == null)
                    continue;

                space.SetValue(SpaceParameter.DesignHeatingLoad, zone.maxHeatingLoad);
                space.SetValue(SpaceParameter.DesignCoolingLoad, zone.maxCoolingLoad);

                adjacencyCluster.AddObject(space);

                List<SpaceSimulationResult> spaceSimulationResults = adjacencyCluster.GetResults<SpaceSimulationResult>(space, Query.Source());
                foreach(LoadType loadType in new LoadType[] {LoadType.Heating, LoadType.Cooling })
                {
                    SpaceSimulationResult spaceSimulationResult = spaceSimulationResults?.Find(x => x.LoadType() == loadType);
                    if(spaceSimulationResult == null)
                    {
                        spaceSimulationResult = Create.SpaceSimulationResult(zone, loadType);
                    }
                    else
                    {
                        spaceSimulationResult.SetValue(Analytical.SpaceSimulationResultParameter.Area, zone.floorArea);
                        spaceSimulationResult.SetValue(Analytical.SpaceSimulationResultParameter.Volume, zone.volume);
                        spaceSimulationResult.SetValue(Analytical.SpaceSimulationResultParameter.DesignLoad, loadType == LoadType.Cooling ? zone.maxCoolingLoad : zone.maxHeatingLoad);
                    }

                    if (spaceSimulationResult != null)
                    {
                        adjacencyCluster.AddObject(spaceSimulationResult);
                        adjacencyCluster.AddRelation(space, spaceSimulationResult);
                    }
                }
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster);

        }
    }
}