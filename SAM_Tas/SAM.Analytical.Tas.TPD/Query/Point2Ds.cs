using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<Point2D> Point2Ds(global::TPD.Duct duct)
        {
            if (duct == null)
            {
                return null;
            }

            int nodeCount = duct.GetNodeCount();
            if (nodeCount < 1)
            {
                return null;
            }

            List<Point2D> result = new List<Point2D>();
            for (int i = 1; i <= nodeCount; i++)
            {
                result.Add(Convert.ToSAM(duct.GetNodePosX(i), duct.GetNodePosY(i)));
            }

            return result;
        }
    }
}