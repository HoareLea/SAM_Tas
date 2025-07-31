using Grasshopper.Kernel.Types;
using SAM.Geometry.Rhino;

namespace SAM.Geometry.Grasshopper
{
    public static partial class Convert
    {
        public static GH_Point ToGrasshopper(this Spatial.Point3D point3D)
        {
            return new GH_Point(point3D.ToRhino());
        }
    }
}
