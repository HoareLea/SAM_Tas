using SAM.Core.Tas;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static void UpdateAdjacencyCluster(this string path_TSD, AdjacencyCluster adjacencyCLuster)
        {
            if (adjacencyCLuster == null || string.IsNullOrWhiteSpace(path_TSD))
                return;

            using (SAMTSDDocument sAMTSDDocument = new SAMTSDDocument(path_TSD, true))
            {
                UpdateAdjacencyCluster(sAMTSDDocument, adjacencyCLuster);
            }
        }

        public static void UpdateAdjacencyCluster(this SAMTSDDocument sAMTSDDocument, AdjacencyCluster adjacencyCLuster)
        {
            if (sAMTSDDocument == null || adjacencyCLuster == null)
                return;

            UpdateAdjacencyCluster(sAMTSDDocument.TSDDocument, adjacencyCLuster);
        }

        public static void UpdateAdjacencyCluster(this TSD.TSDDocument tSDDocument, AdjacencyCluster adjacencyCLuster)
        {
            if (tSDDocument == null || adjacencyCLuster == null)
                return;

            UpdateAdjacencyCluster(tSDDocument.SimulationData, adjacencyCLuster);
        }

        public static void UpdateAdjacencyCluster(this TSD.SimulationData simulationData, AdjacencyCluster adjacencyCluster)
        {
            if (simulationData == null || adjacencyCluster == null)
                return;

            List<SpaceSimulationResult> spaceSimulationResults = Convert.ToSAM(simulationData);
            if (spaceSimulationResults == null || spaceSimulationResults.Count == 0)
                return;

            List<Zone> zones = adjacencyCluster.GetZones();
            if (zones == null)
                return;

            TSD.BuildingData buildingData = simulationData.GetBuildingData();

            foreach (Zone zone in zones)
            {
                List<Space> spaces = adjacencyCluster.GetSpaces(zone);
                if (spaces == null || spaces.Count == 0)
                    continue;

                List<string> references = new List<string>();
                foreach(Space space in spaces)
                {
                    string name = space?.Name;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    SpaceSimulationResult spaceSimulationResult = spaceSimulationResults.Find(x => space.Name.Equals(x.Name));
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
    }
}