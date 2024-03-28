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

            return ToSAM(tasPosition.x, tasPosition.y);
        }

        public static Point2D ToSAM(int x, int y)
        {
            return new Point2D(System.Convert.ToDouble(x) / 100.0, - System.Convert.ToDouble(y) / 100.0);
        }
    }
}
