using SAM.Geometry.Spatial;

namespace SAM.Geometry.Tas
{
    public static partial class Convert
    {
        public static TBD.TasPoint ToTBD(this Point3D point3D)
        {
            if (point3D == null)
            {
                return null;
            }

            TBD.TasPoint result = new TBD.TasPoint();
            result.x = System.Convert.ToSingle(point3D.X);
            result.y = System.Convert.ToSingle(point3D.Y);
            result.z = System.Convert.ToSingle(point3D.Z);

            return result;
        }
    }
}
