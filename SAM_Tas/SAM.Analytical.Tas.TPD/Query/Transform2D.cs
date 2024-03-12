using SAM.Geometry.Planar;
using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static ITransform2D Transform2D(this tpdDirection tpdDirection, Point2D location)
        {
            switch (tpdDirection)
            {
                case tpdDirection.tpdTopBottom:
                    return Geometry.Planar.Transform2D.GetRotation(Math.PI / 2);

                case tpdDirection.tpdLeftRight:
                    return null;

                case tpdDirection.tpdRightLeft:
                    return Geometry.Planar.Transform2D.GetMirrorY(location);

                case tpdDirection.tpdBottomTop:
                    return Geometry.Planar.Transform2D.GetRotation(3 / 2 * Math.PI);
            }

            return null;
        }

        public static ITransform2D Transform2D(this ISystemComponent systemComponent)
        {
            if(systemComponent == null)
            {
                return null;
            }

            dynamic @dynamic = systemComponent;

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            tpdDirection tpdDirection = (tpdDirection)(int)@dynamic.GetDirection();

            return Transform2D(tpdDirection, location);
        }
    }
}