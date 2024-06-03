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

            Point2D location = displaySystemSpace.SystemGeometry?.Location;
            result.SetPosition(location.X, location.Y);

            return result as SystemZone;
        }
    }
}
