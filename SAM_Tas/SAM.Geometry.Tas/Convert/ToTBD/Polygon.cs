using SAM.Geometry.Spatial;
using System.Collections.Generic;

namespace SAM.Geometry.Tas
{
    public static partial class Convert
    {
        public static TBD.Polygon ToTBD(this Polygon3D polygon3D)
        {
            if (polygon3D == null)
            {
                return null;
            }

            TBD.Polygon result = new TBD.Polygon();

            List<Point3D> point3Ds = polygon3D.GetPoints();
            if(point3Ds != null)
            {
                foreach (Point3D point3D in point3Ds)
                {
                    if(point3D == null)
                    {
                        continue;
                    }

                    result.AddCoordinate(System.Convert.ToSingle(point3D.X), System.Convert.ToSingle(point3D.Y), System.Convert.ToSingle(point3D.Z));
                }
            }

            return result;
        }
    }
}
