using Grasshopper.Kernel.Types;
using SAM.Geometry.Rhino;

namespace SAM.Geometry.Grasshopper
{
    public static partial class Convert
    {
        public static GH_Line ToGrasshopper(this Spatial.Segment3D segment3D)
        {
            return new GH_Line(segment3D.ToRhino());
        }
    }
}
