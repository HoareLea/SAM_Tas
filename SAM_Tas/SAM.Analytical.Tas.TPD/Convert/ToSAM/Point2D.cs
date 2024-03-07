using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Point2D ToSAM(this TasPosition tasPosition)
        {
            if (tasPosition == null)
            {
                return null;
            }

            return new Point2D(System.Convert.ToDouble(tasPosition.x) / 100.0, -System.Convert.ToDouble(tasPosition.y) / 100.0);
        }
    }
}
