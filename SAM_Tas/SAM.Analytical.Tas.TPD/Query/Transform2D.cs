using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using SAM.Geometry.Spatial;
using System;
using System.Collections.Generic;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static ITransform2D Transform2D(this tpdDirection tpdDirection, Point2D location, AnalyticalSystemComponentType analyticalSystemComponentType)
        {
            Point2D location_Temp = location;
            location_Temp = new Point2D(location_Temp.X + 0.1, location_Temp.Y - 0.1);
            switch (analyticalSystemComponentType)
            {
                case Analytical.Systems.AnalyticalSystemComponentType.SystemFan:
                    location_Temp = new Point2D(location_Temp.X + 0.3, location_Temp.Y - 0.2);
                    break;
            }


            switch (tpdDirection)
            {
                case tpdDirection.tpdTopBottom:

                    List<ITransform2D> transforms = new List<ITransform2D>()
                    {
                        Geometry.Planar.Transform2D.GetRotation(location_Temp, Math.PI / 2),
                        Geometry.Planar.Transform2D.GetMirrorX(location_Temp),
                    };
                    return new TransformGroup2D(transforms);

                case tpdDirection.tpdLeftRight:
                    return null;

                case tpdDirection.tpdRightLeft:
                    return Geometry.Planar.Transform2D.GetMirrorY(location_Temp);

                case tpdDirection.tpdBottomTop:
                    return Geometry.Planar.Transform2D.GetRotation(location_Temp, Math.PI / 2);
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

            return Transform2D(tpdDirection, location, AnalyticalSystemComponentType(systemComponent));
        }
    }
}