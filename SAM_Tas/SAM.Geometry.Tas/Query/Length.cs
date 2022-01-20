using SAM.Geometry.Spatial;

namespace SAM.Geometry.Tas
{
    public static partial class Query
    {
        public static double Length(this Face3D face3D)
        {
            if(face3D == null)
            {
                return double.NaN;
            }

            Planar.IClosed2D closed2D = face3D.ExternalEdge2D;
            if(closed2D == null)
            {
                return double.NaN;
            }

            Planar.ISegmentable2D segmentable2D = closed2D as Planar.ISegmentable2D;
            if(segmentable2D == null)
            {
                throw new System.NotImplementedException();
            }

            return Planar.Query.MaxDistance(segmentable2D.GetPoints(), out Planar.Point2D point2D_1, out Planar.Point2D point2D_2);
        }
    }
}
