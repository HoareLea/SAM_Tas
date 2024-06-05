using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static Damper ToTPD(this DisplaySystemDamper displaySystemDamper, global::TPD.System system)
        {
            if(displaySystemDamper == null || system == null)
            {
                return null;
            }

            dynamic result = system.AddDamper();

            Point2D point2D = displaySystemDamper.SystemGeometry?.Location?.ToTPD();
            if (point2D != null)
            {
                result.SetPosition(point2D.X, point2D.Y);
            }

            return result as Damper;
        }
    }
}
