using System.Collections.Generic;
using System.Linq;

namespace SAM.Analytical.Tas
{
    public static partial class Query
    {
        public static SpaceSimulationResult SpaceSimulationResult<T>(this IEnumerable<T> results, SpaceSimulationResult spaceSimulationResult) where T : Core.Result
        {
            if (results == null || results.Count() == 0 || spaceSimulationResult == null)
                return null;

            LoadType loadType = Analytical.Query.LoadType(spaceSimulationResult);
            string reference = spaceSimulationResult.Reference;

            foreach(Core.Result result in results)
            {
                SpaceSimulationResult spaceSimulationResult_Temp = result as SpaceSimulationResult;
                if (spaceSimulationResult_Temp == null)
                    continue;

                if (reference != null && spaceSimulationResult_Temp.Reference != reference)
                    continue;

                if (Analytical.Query.LoadType(spaceSimulationResult) != loadType)
                    continue;

                return spaceSimulationResult_Temp;
            }

            return null;
        }
    }
}