using SAM.Core;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    public static partial class Create
    {
        public static AdjacencyCluster AdjacencyCluster(this double elevation, double tolerance = Tolerance.Distance)
        {
            List<Point2D> point2Ds = new List<Point2D>()
            {
                new Point2D(0, 0),
                new Point2D(10, 0),
                new Point2D(10, 10),
                new Point2D(0, 10)

            };

            Polygon2D polygon2D = new Polygon2D(point2Ds);

            double elevation_Min = elevation;
            double elevation_Max = elevation + 3;

            return Analytical.Create.AdjacencyCluster(new List<ISegmentable2D>() { polygon2D }, elevation_Min, elevation_Max, elevation_Max + 1, tolerance);
        }
    }
}