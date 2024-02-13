using SAM.Core.Tas;
using System.Collections.Generic;
using TBD;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static List<string> RemoveVentilationGains(this string path_TBD, AdjacencyCluster adjacencyCluster)
        {
            if (string.IsNullOrWhiteSpace(path_TBD))
                return null;

            List<string> result = null;

            using (SAMTBDDocument sAMTBDDocument = new SAMTBDDocument(path_TBD))
            {
                result = RemoveVentilationGains(sAMTBDDocument, adjacencyCluster);
                if (result != null)
                {
                    sAMTBDDocument.Save();
                }
            }

            return result;
        }

        public static List<string> RemoveVentilationGains(this SAMTBDDocument sAMTBDDocument, AdjacencyCluster adjacencyCluster)
        {
            if (sAMTBDDocument == null)
            {
                return null;
            }

            return RemoveVentilationGains(sAMTBDDocument.TBDDocument, adjacencyCluster);
        }

        public static List<string> RemoveVentilationGains(this TBDDocument tBDDocument, AdjacencyCluster adjacencyCluster)
        {
            if(tBDDocument == null || adjacencyCluster == null)
            {
                return null;
            }

            Building building = tBDDocument.Building;

            List<TBD.InternalCondition> internalConditions = building.InternalConditions();
            if(internalConditions == null || internalConditions.Count == 0)
            {
                return null;
            }

            List<string> result = new List<string>();
            foreach(TBD.InternalCondition internalCondition in internalConditions)
            {
                InternalGain internalGain = internalCondition?.GetInternalGain();
                if(internalGain == null)
                {
                    continue;
                }

                profile profile = internalGain.GetProfile((int)Profiles.ticV);
                if(profile == null)
                {
                    continue;
                }

                result.Add(internalCondition.name);

                profile.type = ProfileTypes.ticValueProfile;
                profile.value = (float)0.0;
                profile.factor = (float)0.0;
                profile.setbackValue = (float)0.0;
                profile.schedule = null;
            }

            return result;
        }
    }
}