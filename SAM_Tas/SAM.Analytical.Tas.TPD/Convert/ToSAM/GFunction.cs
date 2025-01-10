using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static GFunction ToSAM(this ControllerProfileData controllerProfileData)
        {
            if (controllerProfileData == null)
            {
                return null;
            }

            List<Point2D> point2Ds = controllerProfileData.ControllerProfilePoints().ConvertAll(x => new Point2D(x.x, x.y));

            GFunction result = new GFunction(point2Ds);

            return result;
        }
    }
}