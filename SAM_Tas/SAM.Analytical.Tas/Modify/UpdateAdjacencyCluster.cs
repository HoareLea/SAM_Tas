using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<SpaceSimulationResult> UpdateAdjacencyCluster(this string path_TSD, AdjacencyCluster adjacencyCLuster)
        {
            if (adjacencyCLuster == null || string.IsNullOrWhiteSpace(path_TSD))
                return null;

            List<SpaceSimulationResult> result = null;
            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                result = UpdateAdjacencyCluster(sAMTSDDocument, adjacencyCLuster);
            }

            return result;
        }

        public static List<SpaceSimulationResult> UpdateAdjacencyCluster(this SAMTSDDocument sAMTSDDocument, AdjacencyCluster adjacencyCLuster)
        {
            if (sAMTSDDocument == null || adjacencyCLuster == null)
                return null;

            return UpdateAdjacencyCluster(sAMTSDDocument.TSDDocument, adjacencyCLuster);
        }

        public static List<SpaceSimulationResult> UpdateAdjacencyCluster(this TSD.TSDDocument tSDDocument, AdjacencyCluster adjacencyCLuster)
        {
            if (tSDDocument == null || adjacencyCLuster == null)
                return null;

            return UpdateAdjacencyCluster(tSDDocument.SimulationData, adjacencyCLuster);
        }

        public static List<SpaceSimulationResult> UpdateAdjacencyCluster(this TSD.SimulationData simulationData, AdjacencyCluster adjacencyCluster)
        {
            if (simulationData == null || adjacencyCluster == null)
                return null;

            List<SpaceSimulationResult> result = Convert.ToSAM(simulationData);
            if (result == null || result.Count == 0)
                return result;

            Dictionary<System.Guid, List<SpaceSimulationResult>> dictionary = new Dictionary<System.Guid, List<SpaceSimulationResult>>();
            List<Space> spaces = adjacencyCluster.GetSpaces();
            if(spaces != null && spaces.Count > 0)
            {
                foreach (Space space in spaces)
                {
                    List<SpaceSimulationResult> spaceSimulationResults_Space = result.FindAll(x => space.Name.Equals(x.Name));
                    dictionary[space.Guid] = spaceSimulationResults_Space;
                    if(spaceSimulationResults_Space != null && spaceSimulationResults_Space.Count > 0)
                    {
                        foreach (SpaceSimulationResult spaceSimulationResult in spaceSimulationResults_Space)
                        {
                            adjacencyCluster.AddObject(spaceSimulationResult);
                            adjacencyCluster.AddRelation(space, spaceSimulationResult);
                        }
                    }
                }
            }

            List<Zone> zones = adjacencyCluster.GetZones();
            if (zones != null && zones.Count > 0)
            {
                TSD.BuildingData buildingData = simulationData.GetBuildingData();

                foreach (Zone zone in zones)
                {
                    List<Space> spaces_Zone = adjacencyCluster.GetSpaces(zone);
                    if (spaces_Zone == null || spaces_Zone.Count == 0)
                        continue;

                    List<string> references = new List<string>();
                    foreach (Space space in spaces_Zone)
                    {
                        string name = space?.Name;
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        List<SpaceSimulationResult> spaceSimulationResults_Space;
                        if (!dictionary.TryGetValue(space.Guid, out spaceSimulationResults_Space) || spaceSimulationResults_Space == null || spaceSimulationResults_Space.Count == 0)
                            continue;

                        SpaceSimulationResult spaceSimulationResult = spaceSimulationResults_Space[0];
                        if (spaceSimulationResult == null || string.IsNullOrWhiteSpace(spaceSimulationResult.Reference))
                            continue;

                        references.Add(spaceSimulationResult.Reference);
                    }

                    if (!buildingData.TryGetMax(references, TSD.tsdZoneArray.coolingLoad, out int index, out double max) || index == -1 || double.IsNaN(max))
                        continue;

                    zone.SetValue(ZoneParameter.MaxCoolingSensibleLoadIndex, index);
                    zone.SetValue(ZoneParameter.MaxCoolingSensibleLoad, max);
                }
            }

            return result;
        }
    }
}