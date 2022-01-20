using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Geometry.Tas
{
    public static partial class Convert
    {
        public static Polygon3D ToSAM(this TBD.Polygon polygon)
        {
            if(polygon == null)
            {
                return null;
            }

            List<Point3D> point3Ds = new List<Point3D>();

            int index = 0;
            TBD.TasPoint tasPoint = polygon.GetPoint(index);
            while (tasPoint != null)
            {
                Point3D point3D = tasPoint.ToSAM();
                if(point3D != null)
                {
                    point3Ds.Add(point3D);
                }
                index++;

                tasPoint = polygon.GetPoint(index);
            }

            if(point3Ds == null || point3Ds.Count == 0)
            {
                return null;
            }

            return Spatial.Create.Polygon3D(point3Ds);
        }
    }
}
