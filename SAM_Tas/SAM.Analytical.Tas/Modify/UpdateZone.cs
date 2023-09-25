using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static TBD.zone UpdateZone(this TBD.Building building, TBD.zone zone, Space space, ProfileLibrary profileLibrary, AdjacencyCluster adjacencyCluster = null)
        {
            if (space == null || profileLibrary == null || building == null || zone == null)
                return null;

            TBD.InternalCondition internalCondition_TBD = AddInternalCondition(building, space, profileLibrary, adjacencyCluster);
            if (internalCondition_TBD == null)
                return null;

            zone.AssignIC(internalCondition_TBD, true);

            List<string> values = new List<string>();

            //TODO: Update [Id] to [Element Id]
            if (space.TryGetValue("Element Id", out string id))
            {
                if (!string.IsNullOrWhiteSpace(id))
                    values.Add(string.Format("[Id]={0}", id));
            }

            //TODO: Update [LevelName] to [Level Name]
            if(space.TryGetValue(Analytical.SpaceParameter.LevelName, out string levelName))
            {
                if (!string.IsNullOrWhiteSpace(levelName))
                    values.Add(string.Format("[LevelName]={0}", levelName));
            }

            if (values != null && values.Count > 0)
                zone.description = string.Join("; ", values);

            return zone;
        }
    }
}