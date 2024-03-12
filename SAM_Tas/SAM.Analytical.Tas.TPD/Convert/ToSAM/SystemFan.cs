using TPD;
using SAM.Analytical.Systems;
using SAM.Geometry.Planar;
using System;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Convert
    {
        public static SystemFan ToSAM(this Fan fan)
        {
            if (fan == null)
            {
                return null;
            }

            dynamic @dynamic = fan;

            SystemFan systemFan = new SystemFan(@dynamic.Name);
            systemFan.Description = dynamic.Description;
            Modify.SetReference(systemFan, @dynamic.GUID);

            Point2D location = ((TasPosition)@dynamic.GetPosition())?.ToSAM();

            DisplaySystemFan result = Systems.Create.DisplayObject<DisplaySystemFan>(systemFan, location, Systems.Query.DefaultDisplaySystemManager());

            tpdDirection tpdDirection = (tpdDirection)(int)@dynamic.GetDirection();
            ITransform2D transform2D = null;
            switch(tpdDirection)
            {
                case tpdDirection.tpdTopBottom:
                    transform2D = Transform2D.GetRotation(Math.PI / 2);
                    break;

                case tpdDirection.tpdLeftRight:
                    transform2D = null;
                    break;


                case tpdDirection.tpdRightLeft:
                    transform2D = Transform2D.GetMirrorX(location);
                    break;

                case tpdDirection.tpdBottomTop:
                    transform2D = Transform2D.GetRotation(3 / 2 * Math.PI);
                    break;
            }


            if(transform2D != null)
            {
                result.Transform(transform2D);
            }

            return result;
        }
    }
}
