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

            return new Point2D(tasPosition.x, tasPosition.y);
        }
    }
}
