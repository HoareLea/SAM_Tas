using SAM.Geometry.Spatial;

namespace SAM.Geometry.Tas
{
    public static partial class Query
    {
        public static double HydraulicDiameter(this Face3D face3D, double tolerance = Core.Tolerance.Distance)
        {
            Plane plane_Face3D = face3D?.GetPlane();
            if (plane_Face3D == null)
            {
                return double.NaN;
            }
            Plane plane = Plane.WorldXY;

            if(plane.Perpendicular(plane_Face3D, tolerance))
            {
                return 0;
            }

            Face3D face3D_Project = plane.Project(face3D);
            if(face3D_Project == null || !face3D_Project.IsValid())
            {
                return 0;
            }

            Planar.ISegmentable2D segmentable2D = face3D_Project.ExternalEdge2D as Planar.ISegmentable2D;
            if(segmentable2D == null)
            {
                throw new System.NotImplementedException();
            }

            double area = face3D_Project.GetArea();
            if (area <= tolerance)
            {
                return 0;
            }

            //return (4 * area) / segmentable2D.GetLength();
            return segmentable2D.GetLength();
        }
    }
}
