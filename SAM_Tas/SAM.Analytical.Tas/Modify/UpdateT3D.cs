
using SAM.Core.Tas;
using TAS3D;

namespace SAM.Analytical.Tas
{
    public static partial class Modify
    {
        public static bool UpdateT3D(this AdjacencyCluster adjacencyCluster, string path_T3D)
        {
            if (adjacencyCluster == null || string.IsNullOrWhiteSpace(path_T3D))
                return false;

            bool result = false;

            using (SAMT3DDocument sAMT3DDocument = new SAMT3DDocument(path_T3D))
            {
                result = UpdateT3D(adjacencyCluster, sAMT3DDocument.T3DDocument);
                if (result)
                    sAMT3DDocument.Save();
            }

            return result;

        }

        public static bool UpdateT3D(this AdjacencyCluster adjacencyCluster, T3DDocument t3DDocument)
        {
            if (adjacencyCluster == null)
                return false;


            Building building = t3DDocument?.Building;
            if (building == null)
                return false;

            RemoveUnsusedZones(building);


        }
    }
}
