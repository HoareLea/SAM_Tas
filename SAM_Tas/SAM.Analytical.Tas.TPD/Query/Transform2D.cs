using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System;
using TPD;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static ITransform2D Transform2D(this tpdDirection tpdDirection, Point2D location, AnalyticalSystemComponentType analyticalSystemComponentType)
        {

            Point2D location_Temp = location;
            //switch(analyticalSystemComponentType)
            //{
            //    case Analytical.Systems.AnalyticalSystemComponentType.SystemCoolingCoil:
            //    case Analytical.Systems.AnalyticalSystemComponentType.SystemHeatingCoil:
            //        location_Temp = new Point2D(location_Temp.X, location_Temp.Y);
            //        break;
            //}

            switch (tpdDirection)
            {
                case tpdDirection.tpdTopBottom:
                    return new TransformGroup2D(new ITransform2D[] 
                    {
                        Geometry.Planar.Transform2D.GetRotation(location_Temp, Math.PI / 2),
                        Geometry.Planar.Transform2D.GetMirrorX(location_Temp),
                    });

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