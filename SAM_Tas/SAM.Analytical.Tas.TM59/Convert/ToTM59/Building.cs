using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59
{
    public static partial class Convert
    {
        public static Building ToTM59(this AnalyticalModel analyticalModel, TM59Manager tM59Manager)
        {
            if (analyticalModel == null)
            {
                return null;
            }

            List<Zone> zones = new List<Zone>();

            AdjacencyCluster adjacencyCluster = analyticalModel?.AdjacencyCluster;
            if (adjacencyCluster != null)
            {
                List<Space> spaces = adjacencyCluster?.GetSpaces();
                if (spaces != null)
                {
                    foreach (Space space in spaces)
                    {
                        SystemType systemType = SystemType.Undefined;

                        List<VentilationSystem> ventilationSystems = adjacencyCluster.MechanicalSystems<VentilationSystem>(space);
                        if(ventilationSystems != null && ventilationSystems.Count != 0)
                        {
                            VentilationSystem ventilationSystem = ventilationSystems.Find(x => x != null && x.IsMechanicalVentilation());
                            systemType = ventilationSystem == null ? SystemType.NaturalVentilation : SystemType.MechanicalVentilation;
                        }

                        Zone zone = space.ToTM59(tM59Manager, systemType);
                        if (zone != null)
                        {
                            zones.Add(zone);
                        }
                    }
                }
            }

            return new Building(BuildingCategory.Category_II, false, false, zones);
        }
    }
}
