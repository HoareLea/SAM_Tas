using Grasshopper.Kernel.Types;
using SAM.Geometry.Rhino;

namespace SAM.Geometry.Grasshopper
{
    public static partial class Convert
    {
        public static GH_Curve ToGrasshopper(this Spatial.Polygon3D polygon3D)
        {
            return new GH_Curve(((Spatial.ICurve3D)polygon3D).ToRhino());
        }
    }
}
