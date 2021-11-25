using SAM.Core.Tas;
using System.Collections.Generic;
using System.Linq;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<Space> UpdateDesignLoads(this ArchitecturalModel architecturalModel, string path_TBD)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<Space> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = UpdateDesignLoads(architecturalModel, sAMTBDDocument);
                if (result != null)
                    sAMTBDDocument.Save();
            }

            return result;
        }

        public static List<Space> UpdateDesignLoads(this ArchitecturalModel architecturalModel, SAMTBDDocument sAMTBDDocument)
        {
            if (sAMTBDDocument == null)
                return null;

            return UpdateDesignLoads(architecturalModel, sAMTBDDocument.TBDDocument);
        }

        public static List<Space> UpdateDesignLoads(this ArchitecturalModel architecturalModel, TBDDocument tBDDocument)
        {
            if (tBDDocument == null || architecturalModel == null)
                return null;

            Building building = tBDDocument.Building;
            if (building == null)
                return null;

            List<Space> spaces = architecturalModel.GetSpaces();
            if (spaces == null || spaces.Count == 0)
                return spaces;

            Dictionary<string, zone> zones = building.ZoneDictionary();
            if (zones == null)
                return null;

            List<Space> result = new List<Space>();
            foreach (Space space in spaces)
            {
                string name = space?.Name;
                if (string.IsNullOrEmpty(name))
                    continue;

                zone zone;
                if (!zones.TryGetValue(name, out zone) || zone == null)
                    continue;

                result.Add(space);

                space.SetValue(SpaceParameter.DesignHeatingLoad, zone.maxHeatingLoad);
                space.SetValue(SpaceParameter.DesignCoolingLoad, zone.maxCoolingLoad);

                architecturalModel.Add(space);

                List<SpaceSimulationResult> spaceSimulationResults = architecturalModel.GetResults<SpaceSimulationResult>(space, Query.Source());
                foreach(LoadType loadType in new LoadType[] {LoadType.Heating, LoadType.Cooling })
                {
                    SpaceSimulationResult spaceSimulationResult = spaceSimulationResults?.Find(x => x.LoadType() == loadType);
                    if(spaceSimulationResult == null)
                    {
                        spaceSimulationResult = Create.SpaceSimulationResult(zone, loadType);
                    }
                    else
                    {
                        spaceSimulationResult.SetValue(SpaceSimulationResultParameter.Area, zone.floorArea);
                        spaceSimulationResult.SetValue(SpaceSimulationResultParameter.Volume, zone.volume);
                        spaceSimulationResult.SetValue(SpaceSimulationResultParameter.DesignLoad, loadType == LoadType.Cooling ? zone.maxCoolingLoad : zone.maxHeatingLoad);
                    }

                    if (spaceSimulationResult != null)
                    {
                        architecturalModel.Add(spaceSimulationResult, space);
                    }
                }
            }

            return result;

        }
    }
}