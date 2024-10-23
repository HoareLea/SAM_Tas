using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static BoundingBox2D BoundingBox2D(this PlantGroup plantGroup)
        {
            if (plantGroup == null)
            {
                return null;
            }

            dynamic @dynamic = plantGroup;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            Point2D min = location;
            Point2D max = new Point2D(min.X + 0.6, min.Y + 1);

            return new BoundingBox2D(min, max);
        }
    }
}