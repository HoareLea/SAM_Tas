using SAM.Geometry.Spatial;

namespace SAM.Geometry.Tas
{
    public static partial class Convert
    {
        public static Point3D ToSAM(this TBD.TasPoint tasPoint)
        {
            if(tasPoint == null)
            {
                return null;
            }

            return new Point3D(tasPoint.x, tasPoint.y, tasPoint.z);
        }
    }
}
