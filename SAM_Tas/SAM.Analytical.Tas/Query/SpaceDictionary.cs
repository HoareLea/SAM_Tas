using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, Space> SpaceDictionary(this AdjacencyCluster adjacencyCluster)
        {
            List<Space> spaces = adjacencyCluster?.GetSpaces();
            if (spaces == null)
                return null;

            Dictionary<string, Space> result = new Dictionary<string, Space>();
            foreach(Space space in spaces)
            {
                string name = space?.Name;
                if (name == null)
                    continue;

                result[name] = space;
            }

            return result;
        }
    }
}