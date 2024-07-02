using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static Dictionary<string, T> SpaceDictionary<T>(this AdjacencyCluster adjacencyCluster) where T : ISpace
        {
            List<T> spaces = adjacencyCluster?.GetObjects<T>();
            if (spaces == null)
                return null;

            Dictionary<string, T> result = new Dictionary<string, T>();
            foreach(T space in spaces)
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