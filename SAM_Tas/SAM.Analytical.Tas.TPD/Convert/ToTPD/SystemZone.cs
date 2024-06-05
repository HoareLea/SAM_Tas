using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemZone ToTPD(this DisplaySystemSpace displaySystemSpace, global::TPD.System system)
        {
            if(displaySystemSpace == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddSystemZone();

            Point2D point2D = displaySystemSpace.SystemGeometry?.Location?.ToTPD();
            if (point2D != null)
            {
                result.SetPosition(point2D.X, point2D.Y);
            }

            return result as SystemZone;
        }
    }
}
